//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using System;
using UnityEngine;

namespace Keramzit
{
    public class KzNodeNumberTweaker : PartModule
    {
        [KSPField] public string nodePrefix = "bottom";
        [KSPField] public int maxNumber;

        [KSPField (guiActiveEditor = true, guiName = "Fairing Nodes")]
        [UI_FloatRange (minValue = 1, maxValue = 8, stepIncrement = 1)]
        public float uiNumNodes = 2;

        [KSPField (isPersistant = true)]
        public int numNodes = 2;

        int numNodesBefore;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Node offset", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 0.625f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float radius = 1.25f;

        [KSPField] public float radiusStepLarge = 0.625f;
        [KSPField] public float radiusStepSmall = 0.125f;

        [KSPField] public bool shouldResizeNodes = true;

        protected float oldRadius = -1000;
        protected bool justLoaded;

        public override string GetInfo ()
        {
            return "Max. Nodes: " + maxNumber;
        }

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Interstage Nodes")]
        [UI_Toggle (disabledText = "Off", enabledText = "On")]
        public bool showInterstageNodes = true;

        public virtual void FixedUpdate ()
        {
            if (!radius.Equals (oldRadius))
            {
                oldRadius = radius;

                updateNodePositions ();
            }

            justLoaded = false;
        }

        public void Update ()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if ((int) uiNumNodes != numNodesBefore)
                {
                    if (checkNodeAttachments ())
                    {
                        uiNumNodes = numNodesBefore;
                    }
                    else
                    {
                        numNodes = (int) uiNumNodes;
                        numNodesBefore = numNodes;

                        updateNodes ();

                        bool removed = false;

                        for (int i = numNodes + 1; i <= maxNumber; ++i)
                        {
                            var node = findNode (i);

                            if (node == null)
                            {
                                continue;
                            }

                            //  Fix for ghost node when inserting a new PF base in
                            //  the editor scene: do not delete unused nodes, move
                            //  them away instead. Be careful to check references
                            //  of the maximum number of nodes, mentioned elsewhere
                            //  and retrieved via 'Findattachnodes("connect")'!
                            //  Slightly hacky, but it works...

                            HideUnusedNode (node);

                            removed = true;
                        }

                        if (removed)
                        {
                            var fbase = part.GetComponent<ProceduralFairingBase>();

                            if (fbase)
                            {
                                fbase.needShapeUpdate = true;
                                fbase.updateDelay = 0.5f;
                            }
                        }
                    }
                }
            }
        }

        //  Slightly hacky...but it removes the ghost nodes.

        void HideUnusedNode (AttachNode node)
        {
            node.position.x = 10000;
        }

        public override void OnStart (StartState state)
        {
            base.OnStart (state);

            if (state == StartState.None)
            {
                return;
            }

            ((UI_FloatEdit) Fields["radius"].uiControlEditor).incrementLarge = radiusStepLarge;
            ((UI_FloatEdit) Fields["radius"].uiControlEditor).incrementSmall = radiusStepSmall;

            Fields["radius"].guiActiveEditor = shouldResizeNodes;

            //  Hide the interstage toggle button if there are no interstage nodes.

            var nodes = part.FindAttachNodes ("interstage");

            if (nodes == null)
            {
                Fields["showInterstageNodes"].guiActiveEditor = false;
            }

            //  Change the GUI text if there are no fairing attachment nodes.

            nodes = part.FindAttachNodes ("connect");

            if (nodes == null)
            {
                Fields["uiNumNodes"].guiName = "Side Nodes";
            }

            ((UI_FloatRange) Fields["uiNumNodes"].uiControlEditor).maxValue = maxNumber;

            uiNumNodes = numNodes;
            numNodesBefore = numNodes;

            updateNodes ();
        }

        public override void OnLoad (ConfigNode cfg)
        {
            base.OnLoad (cfg);

            justLoaded = true;

            uiNumNodes = numNodes;
            numNodesBefore = numNodes;

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                updateNodes ();
            }
        }

        void updateNodes ()
        {
            addRemoveNodes ();

            updateNodePositions ();
        }

        string nodeName (int i)
        {
            return string.Format ("{0}{1:d2}", nodePrefix, i);
        }

        AttachNode findNode (int i)
        {
            return part.FindAttachNode (nodeName (i));
        }

        bool checkNodeAttachments()
        {
            for (int i = 1; i <= maxNumber; ++i)
            {
                var node = findNode (i);

                if (node != null && node.attachedPart != null)
                {
                    EditorScreenMessager.showMessage ("Please detach any fairing parts before changing the number of nodes!", 1);

                    return true;
                }
            }

            return false;
        }

        void addRemoveNodes ()
        {
            part.stackSymmetry = numNodes - 1;

            float y = 0;

            bool gotY = false;

            int nodeSize = 0;

            Vector3 dir = Vector3.up;

            int i;

            for (i = 1; i <= maxNumber; ++i)
            {
                var node = findNode (i);

                if (node == null)
                {
                    continue;
                }

                y = node.position.y;
                nodeSize = node.size;
                dir = node.orientation;

                gotY = true;

                break;
            }

            if (!gotY)
            {
                var node = part.FindAttachNode ("bottom");

                if (node != null)
                {
                    y = node.position.y;
                }
            }

            for (i = 1; i <= numNodes; ++i)
            {
                var node = findNode (i);

                if (node != null)
                {
                    continue;
                }

                //  Create the fairing attachment node.

                node = new AttachNode ();

                node.id = nodeName (i);
                node.owner = part;
                node.nodeType = AttachNode.NodeType.Stack;
                node.position = new Vector3 (0, y, 0);
                node.orientation = dir;
                node.originalPosition = node.position;
                node.originalOrientation = node.orientation;
                node.size = nodeSize;

                part.attachNodes.Add (node);
            }

            for (; i <= maxNumber; ++i)
            {
                var node = findNode (i);

                if (node == null)
                {
                    continue;
                }

                if (HighLogic.LoadedSceneIsEditor)
                {
                    node.position.x = 10000;
                }
                else
                {
                    part.attachNodes.Remove (node);
                }
            }

            var fbase = part.GetComponent<ProceduralFairingBase>();

            if (fbase)
            {
                fbase.needShapeUpdate = true;
            }
        }

        void updateNodePositions ()
        {
            float d = Mathf.Sin (Mathf.PI / numNodes) * radius * 2;

            int size = Mathf.RoundToInt (d / (radiusStepLarge * 2));

            for (int i = 1; i <= numNodes; ++i)
            {
                var node = findNode (i);

                if (node == null)
                {
                    continue;
                }

                float a = Mathf.PI * 2 * (i - 1) / numNodes;

                node.position.x = Mathf.Cos (a) * radius;
                node.position.z = Mathf.Sin (a) * radius;

                if (shouldResizeNodes)
                {
                    node.size = size;
                }

                if (!justLoaded)
                {
                    PFUtils.updateAttachedPartPos (node, part);
                }
            }

            for (int i = numNodes + 1; i <= maxNumber; ++i)
            {
                var node = findNode (i);

                if (node == null)
                {
                    continue;
                }

                node.position.x = 10000;
            }
        }
    }
}
