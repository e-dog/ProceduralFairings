//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Keramzit
{
    abstract class ProceduralAdapterBase : PartModule
    {
        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Base", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 1.25f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float baseSize = 1.25f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Top", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 1.25f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float topSize = 1.25f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Height", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 50, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float height = 1;

        [KSPField] public string topNodeName = "top1";

        [KSPField] public float diameterStepLarge = 1.25f;
        [KSPField] public float diameterStepSmall = 0.125f;

        [KSPField] public float heightStepLarge = 1.0f;
        [KSPField] public float heightStepSmall = 0.1f;

        public bool changed = true;

        abstract public float minHeight { get; }

        float lastBaseSize = -1000;
        float lastTopSize = -1000;
        float lastHeight = -1000;

        protected bool justLoaded;

        public virtual void checkTweakables ()
        {
            if (!baseSize.Equals (lastBaseSize))
            {
                lastBaseSize = baseSize;

                changed = true;
            }

            if (!topSize.Equals (lastTopSize))
            {
                lastTopSize = topSize;

                changed = true;
            }

            if (!height.Equals (lastHeight))
            {
                lastHeight = height;

                changed = true;
            }
        }

        public virtual void FixedUpdate ()
        {
            checkTweakables ();

            if (changed)
            {
                updateShape();
            }

            justLoaded = false;
        }

        public virtual void updateShape ()
        {
            changed = false;

            float topheight = 0;
            float topnodeheight = 0;

            var node = part.FindAttachNode ("bottom");

            if (node != null)
            {
                node.size = Mathf.RoundToInt(baseSize / diameterStepLarge);
            }

            node = part.FindAttachNode ("top");

            if (node != null)
            {
                node.size = Mathf.RoundToInt (baseSize / diameterStepLarge);

                topheight = node.position.y;
            }

            node = part.FindAttachNode (topNodeName);

            if (node != null)
            {
                node.position = new Vector3 (0, height, 0);

                node.size = Mathf.RoundToInt (topSize / diameterStepLarge);

                if (!justLoaded)
                {
                    PFUtils.updateAttachedPartPos (node, part);
                }

                topnodeheight = height;
            }
            else
            {
                Debug.LogError ("[PF]: No '" + topNodeName + "' node in part!", this);
            }

            var internodes = part.FindAttachNodes ("interstage");

            if (internodes != null)
            {
                var inc = (topnodeheight - topheight) / (internodes.Length / 2 + 1);

                for (int i = 0, j = 0; i < internodes.Length; i = i + 2)
                {
                    var height = topheight + (j + 1) * inc;

                    j++;

                    node = internodes [i];

                    node.position.y = height;
                    node.size = node.size = Mathf.RoundToInt (topSize / diameterStepLarge) - 1;

                    if (!justLoaded)
                    {
                        PFUtils.updateAttachedPartPos (node, part);
                    }

                    node = internodes [i + 1];

                    node.position.y = height;
                    node.size = node.size = Mathf.RoundToInt (topSize / diameterStepLarge) - 1;

                    if (!justLoaded)
                    {
                        PFUtils.updateAttachedPartPos (node, part);
                    }
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart (state);

            if (state == StartState.None)
            {
                return;
            }

            StartCoroutine (FireFirstChanged ());

        }

        public IEnumerator<YieldInstruction> FireFirstChanged ()
        {
            while(!(part.editorStarted || part.started))
            {
                yield return new WaitForFixedUpdate ();
            }

            //  Wait a little more...

            yield return new WaitForSeconds (.01f);

            changed = true;
        }

        public override void OnLoad (ConfigNode cfg)
        {
            base.OnLoad (cfg);

            justLoaded = true;
            changed = true;
        }
    }

    class ProceduralFairingAdapter : ProceduralAdapterBase, IPartCostModifier, IPartMassModifier
    {
        [KSPField] public float sideThickness = 0.05f / 1.25f;
        [KSPField] public Vector4 specificMass = new Vector4 (0.005f, 0.011f, 0.009f, 0f);
        [KSPField] public float specificBreakingForce = 6050;
        [KSPField] public float specificBreakingTorque = 6050;
        [KSPField] public float costPerTonne = 2000;

        [KSPField] public float dragAreaScale = 1;

        [KSPField (isPersistant = true)]
        public bool topNodeDecouplesWhenFairingsGone;

        public bool isTopNodePartPresent = true;
        public bool isFairingPresent = true;

        [KSPEvent (name = "decNoFairings", active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, guiName = "text")]
        public void UIToggleTopNodeDecouple ()
        {
            topNodeDecouplesWhenFairingsGone = !topNodeDecouplesWhenFairingsGone;

            UpdateUIdecNoFairingsText (topNodeDecouplesWhenFairingsGone);
        }

        void UpdateUIdecNoFairingsText (bool flag)
        {
            if (flag)
            {
                Events["UIToggleTopNodeDecouple"].guiName = "Decouple when Fairing gone: Yes";
            }
            else
            {
                Events["UIToggleTopNodeDecouple"].guiName = "Decouple when Fairing gone: No";
            }
        }

        public override float minHeight
        {
            get
            {
                return baseSize * 0.2f;
            }
        }

        public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.FIXED; }
        public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.FIXED; }

        public float GetModuleCost (float defcost, ModifierStagingSituation sit)
        {
            return totalMass * costPerTonne - defcost;
        }

        public float GetModuleMass (float defmass, ModifierStagingSituation sit)
        {
            return totalMass - defmass;
        }

        public float calcSideThickness ()
        {
            return Mathf.Min (sideThickness * Mathf.Max (baseSize, topSize), Mathf.Min (baseSize, topSize) * 0.25f);
        }

        public float topRadius
        {
            get
            {
                return topSize * 0.5f - calcSideThickness ();
            }
        }

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Extra height", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0, maxValue = 50, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float extraHeight = 0;

        public bool engineFairingRemoved;

        [KSPField (isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Mass")]
        public string massDisplay;

        [KSPField (isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Cost")]
        public string costDisplay;

        bool limitsSet;

        float lastExtraHt = -1000;

        public override void checkTweakables ()
        {
            base.checkTweakables ();

            if (!extraHeight.Equals (lastExtraHt))
            {
                lastExtraHt = extraHeight;

                changed = true;
            }
        }

        void RemoveTopPartJoints ()
        {
            Part topPart = getTopPart ();

            Part bottomPart = getBottomPart ();

            if ((topPart == null ? false : (bottomPart != null)))
            {
                ConfigurableJoint [] components = topPart.gameObject.GetComponents<ConfigurableJoint>();

                for (int i = 0; i < (int) components.Length; i++)
                {
                    ConfigurableJoint configurableJoint = components [i];

                    if (configurableJoint.connectedBody == bottomPart.Rigidbody)
                    {
                        UnityEngine.Object.Destroy (configurableJoint);
                    }
                }
            }
        }

        public void Start ()
        {
            part.mass = totalMass;
        }

        public override void OnStart (StartState state)
        {
            base.OnStart (state);

            limitsSet = false;

            part.mass = totalMass;

            isFairingPresent = CheckForFairingPresent ();

            isTopNodePartPresent = (getTopPart () != null);

            UpdateUIdecNoFairingsText (topNodeDecouplesWhenFairingsGone);

            GameEvents.onEditorShipModified.Add (OnEditorShipModified);
            GameEvents.onVesselWasModified.Add (OnVesselWasModified);
            GameEvents.onVesselCreate.Add (OnVesselCreate);
            GameEvents.onVesselGoOffRails.Add (OnVesselGoOffRails);
            GameEvents.onVesselLoaded.Add (OnVesselLoaded);
            GameEvents.onStageActivate.Add (OnStageActivate);
        }

        public void OnDestroy ()
        {
            GameEvents.onEditorShipModified.Remove (OnEditorShipModified);
            GameEvents.onVesselWasModified.Remove (OnVesselWasModified);
            GameEvents.onVesselCreate.Remove (OnVesselCreate);
            GameEvents.onVesselGoOffRails.Remove (OnVesselGoOffRails);
            GameEvents.onVesselLoaded.Remove (OnVesselLoaded);
            GameEvents.onStageActivate.Remove (OnStageActivate);
        }

        bool isShipModified = true;
        bool isStaged;

        int stageNum;

        //  Lets catch some events...

        void OnEditorShipModified (ShipConstruct sc)
        {
            isShipModified = true;
        }

        void OnVesselWasModified (Vessel ves)
        {
            isShipModified = true;
        }

        void OnVesselCreate (Vessel ves)
        {
            isShipModified = true;
        }

        void OnVesselGoOffRails (Vessel ves)
        {
            isShipModified = true;
        }

        void OnVesselLoaded (Vessel ves)
        {
            isShipModified = true;
        }

        void OnStageActivate (int stage)
        {
            isStaged = true;

            stageNum = stage;
        }

        public float totalMass;

        public override void updateShape ()
        {
            base.updateShape ();

            float sth = calcSideThickness ();

            float br = baseSize * 0.5f - sth;
            float scale = br * 2;

            part.mass = totalMass = ((specificMass.x * scale + specificMass.y) * scale + specificMass.z) * scale + specificMass.w;

            massDisplay = PFUtils.formatMass (totalMass);
            costDisplay = PFUtils.formatCost (part.partInfo.cost + GetModuleCost (part.partInfo.cost, ModifierStagingSituation.CURRENT));

            part.breakingForce = specificBreakingForce * Mathf.Pow (br, 2);
            part.breakingTorque = specificBreakingTorque * Mathf.Pow (br, 2);

            var model = part.FindModelTransform ("model");

            if (model != null)
            {
                model.localScale = Vector3.one * scale;
            }
            else
            {
                Debug.LogError("[PF]: No 'model' transform found in part!", this);
            }

            part.rescaleFactor = scale;

            var node = part.FindAttachNode ("top");

            node.position = node.originalPosition * scale;

            if (!justLoaded)
            {
                PFUtils.updateAttachedPartPos (node, part);
            }

            var topNode = part.FindAttachNode ("top");
            var bottomNode = part.FindAttachNode ("bottom");

            float y = (topNode.position.y + bottomNode.position.y) * 0.5f;

            int sideNodeSize = Mathf.RoundToInt(scale / diameterStepLarge) - 1;

            if (sideNodeSize < 0)
            {
                sideNodeSize = 0;
            }

            var nodes = part.FindAttachNodes ("connect");

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    var n = nodes [i];

                    n.position.y = y;
                    n.size = sideNodeSize;

                    if (!justLoaded)
                    {
                        PFUtils.updateAttachedPartPos (n, part);
                    }
                }
            }

            var topnode2 = part.FindAttachNode (topNodeName);
            var internodes = part.FindAttachNodes ("interstage");

            if (internodes != null && topnode2 != null)
            {
                var topheight = topNode.position.y;
                var topnode2height = topnode2.position.y;

                var inc = (topnode2height - topheight) / (internodes.Length / 2 + 1);

                for (int i = 0, j = 0; i < internodes.Length; i = i + 2)
                {
                    var height = topheight + (j + 1) * inc;

                    j++;

                    node = internodes [i];

                    node.position.y = height;
                    node.size = topNode.size;

                    if (!justLoaded)
                    {
                        PFUtils.updateAttachedPartPos (node, part);
                    }

                    node = internodes[i + 1];

                    node.position.y = height;
                    node.size = sideNodeSize;

                    if (!justLoaded)
                    {
                        PFUtils.updateAttachedPartPos (node, part);
                    }
                }
            }

            var nnt = part.GetComponent<KzNodeNumberTweaker>();

            if (nnt)
            {
                nnt.radius = baseSize * 0.5f;
            }

            var fbase = part.GetComponent<ProceduralFairingBase>();

            if (fbase)
            {
                fbase.baseSize = br * 2;
                fbase.sideThickness = sth;

                fbase.needShapeUpdate = true;
            }

            StartCoroutine (PFUtils.updateDragCubeCoroutine(part, dragAreaScale));
        }

        public override void FixedUpdate ()
        {
            base.FixedUpdate ();

            if (!limitsSet && PFUtils.canCheckTech ())
            {
                limitsSet = true;

                float minSize = PFUtils.getTechMinValue ("PROCFAIRINGS_MINDIAMETER", 0.25f);
                float maxSize = PFUtils.getTechMaxValue ("PROCFAIRINGS_MAXDIAMETER", 30);

                PFUtils.setFieldRange (Fields["baseSize"], minSize, maxSize);
                PFUtils.setFieldRange (Fields["topSize"], minSize, maxSize);

                ((UI_FloatEdit) Fields["baseSize"].uiControlEditor).incrementLarge = diameterStepLarge;
                ((UI_FloatEdit) Fields["baseSize"].uiControlEditor).incrementSmall = diameterStepSmall;
                ((UI_FloatEdit) Fields["topSize"].uiControlEditor).incrementLarge = diameterStepLarge;
                ((UI_FloatEdit) Fields["topSize"].uiControlEditor).incrementSmall = diameterStepSmall;

                ((UI_FloatEdit) Fields["height"].uiControlEditor).incrementLarge = heightStepLarge;
                ((UI_FloatEdit) Fields["height"].uiControlEditor).incrementSmall = heightStepSmall;
                ((UI_FloatEdit) Fields["extraHeight"].uiControlEditor).incrementLarge = heightStepLarge;
                ((UI_FloatEdit) Fields["extraHeight"].uiControlEditor).incrementSmall = heightStepSmall;
            }

            if (isShipModified)
            {
                isShipModified = false;

                //  Remove the engine fairing (if there is any) from topmost node.

                if (!engineFairingRemoved)
                {
                    var node = part.FindAttachNode (topNodeName);

                    if (node != null && node.attachedPart != null)
                    {
                        var tp = node.attachedPart;

                        if (HighLogic.LoadedSceneIsEditor || !tp.packed)
                        {
                            var comps = tp.GetComponents<ModuleJettison>();

                            for (int i = 0; i < comps.Length; i++)
                            {
                                var mj = comps [i];

                                var jt = tp.FindModelTransform (mj.jettisonName);

                                if (jt == null)
                                {
                                    jt = mj.jettisonTransform;
                                }

                                if (jt != null)
                                {
                                    jt.gameObject.SetActive (false);
                                }

                                mj.jettisonName = null;
                                mj.jettisonTransform = null;
                            }

                            if (!HighLogic.LoadedSceneIsEditor)
                            {
                                engineFairingRemoved = true;
                            }
                        }
                    }
                }

                if (!HighLogic.LoadedSceneIsEditor)
                {
                    if (isTopNodePartPresent)
                    {
                        var tp = getTopPart ();

                        if (tp == null)
                        {
                            isTopNodePartPresent = false;

                            Events["UIToggleTopNodeDecouple"].guiActive = false;
                        }
                        else
                        {
                            if (topNodeDecouplesWhenFairingsGone && !CheckForFairingPresent ())
                            {
                                PartModule item = part.Modules["ModuleDecouple"];

                                if (item == null)
                                {
                                    Debug.LogError ("[PF]: Cannot decouple from top part!", this);
                                }
                                else
                                {
                                    RemoveTopPartJoints ();

                                    ((ModuleDecouple)item).Decouple ();

                                    part.stackIcon.RemoveIcon ();

                                    StageManager.Instance.SortIcons (true);

                                    isFairingPresent = false;
                                    isTopNodePartPresent = false;

                                    Events["UIToggleTopNodeDecouple"].guiActive = false;
                                }
                            }
                        }
                    }

                    if (isStaged)
                    {
                        isStaged = false;

                        if (part != null)
                        {
                            if (stageNum == part.inverseStage)
                            {
                                part.stackIcon.RemoveIcon ();

                                StageManager.Instance.SortIcons (true);

                                Events["UIToggleTopNodeDecouple"].guiActive = false;
                            }
                        }
                    }
                }
            }
        }

        public Part getBottomPart ()
        {
            Part part;

            AttachNode attachNode = base.part.FindAttachNode ("bottom");

            if (attachNode != null)
            {
                part = attachNode.attachedPart;
            }
            else
            {
                part = null;
            }

            return part;
        }

        public bool CheckForFairingPresent()
        {
            if (!isFairingPresent)
            {
                return false;
            }

            var nodes = part.FindAttachNodes ("connect");

            if (nodes == null)
            {
                return false;
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes [i];

                if (n.attachedPart != null)
                {
                    return true;
                }
            }

            return false;
        }

        public Part getTopPart ()
        {
            var node = part.FindAttachNode (topNodeName);

            if (node == null)
            {
                return null;
            }

            return node.attachedPart;
        }

        public override void OnLoad (ConfigNode cfg)
        {
            base.OnLoad (cfg);

            if (cfg.HasValue ("baseRadius") && cfg.HasValue ("topRadius"))
            {
                //  Load legacy settings.

                float br = float.Parse (cfg.GetValue ("baseRadius"));
                float tr = float.Parse (cfg.GetValue ("topRadius"));

                baseSize = (br + sideThickness * br) * 2;
                topSize = (tr + sideThickness * br) * 2;

                sideThickness *= 1.15f / 1.25f;
            }
        }
    }
}
