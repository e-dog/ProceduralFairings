//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Keramzit
{
    public class PFKMJoints : PartModule
    {
        [KSPField (isPersistant = true)]
        public float breakingForce = 500000f;

        [KSPField (guiActive = true, guiName = "View Joints")]
        [UI_Toggle (disabledText = "Off", enabledText = "On")]
        public bool viewJoints;

        const float w1 = 0.05f;
        const float w2 = 0.15f;

        readonly List<LineRenderer> jointLines = new List<LineRenderer>();

        bool morejointsadded;

        Part bottomNodePart;

        List<Part> nodeParts = new List<Part>();

        void AddMoreJoints ()
        {
            int i;

            AttachNode attachNode;

            if (!morejointsadded)
            {
                AttachNode attachNode1 = base.part.FindAttachNode ("bottom");

                AttachNode [] attachNodeArray = base.part.FindAttachNodes ("interstage");
                AttachNode [] attachNodeArray1 = base.part.FindAttachNodes ("top");

                string [] _vessel = { "[PF]: Adding Joints to Vessel: ", vessel.vesselName, " (Launch ID: ", base.part.launchID.ToString(), ") (GUID: ", base.vessel.id.ToString(), ")" };

                Debug.Log (string.Concat (_vessel));

                _vessel = new [] { "[PF]: For PF Part: ", base.part.name, " (", base.part.craftID.ToString (), ")" };

                Debug.Log (string.Concat (_vessel));

                Part part = null;

                if ((attachNode1 == null ? false : attachNode1.attachedPart != null))
                {
                    part = attachNode1.attachedPart;

                    bottomNodePart = part;

                    addStrut (part, base.part, Vector3.zero);

                    _vessel = new [] { "[PF]: Bottom Part: ", part.name, " (", part.craftID.ToString (), ")" };

                    Debug.Log (string.Concat (_vessel));
                }

                Debug.Log ("[PF]: Top Parts:");

                if (attachNodeArray1 != null)
                {
                    for (i = 0; i < (int)attachNodeArray1.Length; i++)
                    {
                        attachNode = attachNodeArray1 [i];

                        if (attachNode.attachedPart != null)
                        {
                            if (part != null)
                            {
                                AddPartJoint (attachNode.attachedPart, part, attachNode.FindOpposingNode (), attachNode1.FindOpposingNode ());
                            }

                            addStrut (attachNode.attachedPart, base.part, Vector3.zero);

                            nodeParts.Add (attachNode.attachedPart);

                            _vessel = new [] { "[PF]:", attachNode.attachedPart.name, " (", attachNode.attachedPart.craftID.ToString (), ")" };

                            Debug.Log (string.Concat (_vessel));
                        }
                    }
                }

                if (attachNodeArray != null)
                {
                    for (i = 0; i < (int)attachNodeArray.Length; i++)
                    {
                        attachNode = attachNodeArray [i];

                        if (attachNode.attachedPart != null)
                        {
                            if (part != null)
                            {
                                AddPartJoint (attachNode.attachedPart, part, attachNode.FindOpposingNode (), attachNode1.FindOpposingNode ());
                            }

                            addStrut (attachNode.attachedPart, base.part, Vector3.zero);

                            nodeParts.Add (attachNode.attachedPart);

                            _vessel = new [] { "[PF]:", attachNode.attachedPart.name, " (", attachNode.attachedPart.craftID.ToString (), ")" };

                            Debug.Log (string.Concat (_vessel));
                        }
                    }
                }

                morejointsadded = true;
            }
        }

        void AddPartJoint (Part p, Part pp, AttachNode pnode, AttachNode ppnode)
        {
            PartJoint partJoint = PartJoint.Create (p, pp, pnode, ppnode, 0);

            partJoint.SetBreakingForces (breakingForce, breakingForce);

            PartJoint partJoint1 = p.gameObject.AddComponent<PartJoint>();

            partJoint1 = partJoint;
        }

        ConfigurableJoint addStrut (Part p, Part pp, Vector3 pos)
        {
            ConfigurableJoint configurableJoint;

            if (p != pp)
            {
                Rigidbody rigidbody = pp.Rigidbody;

                if ((rigidbody == null ? false : !(rigidbody == p.Rigidbody)))
                {
                    ConfigurableJoint configurableJoint1 = p.gameObject.AddComponent<ConfigurableJoint>();

                    configurableJoint1.xMotion = 0;
                    configurableJoint1.yMotion = 0;
                    configurableJoint1.zMotion = 0;
                    configurableJoint1.angularXMotion = 0;
                    configurableJoint1.angularYMotion = 0;
                    configurableJoint1.angularZMotion = 0;
                    configurableJoint1.projectionDistance = 0.1f;
                    configurableJoint1.projectionAngle = 5f;
                    configurableJoint1.breakForce = breakingForce;
                    configurableJoint1.breakTorque = breakingForce;
                    configurableJoint1.connectedBody = rigidbody;
                    configurableJoint1.targetPosition = pos;
                    configurableJoint = configurableJoint1;
                }
                else
                {
                    configurableJoint = null;
                }
            }
            else
            {
                configurableJoint = null;
            }

            return configurableJoint;
        }

        void ClearJointLines ()
        {
            for (int i = 0; i < jointLines.Count; i++)
            {
                UnityEngine.Object.Destroy (jointLines [i].gameObject);
            }

            jointLines.Clear ();
        }

        public virtual void FixedUpdate ()
        {
            if (!morejointsadded)
            {
                if ((!FlightGlobals.ready || vessel.packed ? false : vessel.loaded))
                {
                    AddMoreJoints ();
                }
            }
        }

        LineRenderer JointLine (Vector3 posp, Vector3 pospp, Color col, float width)
        {
            LineRenderer lineRenderer = makeLineRenderer ("JointLine", col, width);

            lineRenderer.positionCount = 2;

            lineRenderer.SetPosition (0, posp);
            lineRenderer.SetPosition (1, pospp);

            lineRenderer.useWorldSpace = true;

            return lineRenderer;
        }

        public void ListJoints ()
        {
            int j;

            string [] _name;

            float _breakForce;

            Vector3 _anchor;

            string str;
            string str1;

            ClearJointLines ();

            List<Part> activeVessel = FlightGlobals.ActiveVessel.parts;

            for (int i = 0; i < activeVessel.Count; i++)
            {
                ConfigurableJoint [] components = activeVessel[i].gameObject.GetComponents<ConfigurableJoint>();

                if (components != null)
                {
                    for (j = 0; j < (int)components.Length; j++)
                    {
                        ConfigurableJoint configurableJoint = components [j];

                        _name = new string [18];

                        _name[0] = "[PF]: <ConfigurableJoint>, ";
                        _name[1] = activeVessel [i].name;
                        _name[2] = ", ";
                        _name[3] = (configurableJoint.connectedBody == null ? "<none>" : configurableJoint.connectedBody.name);
                        _name[4] = ", ";
                        _breakForce = configurableJoint.breakForce;
                        _name[5] = _breakForce.ToString ();
                        _name[6] = ", ";
                        _breakForce = configurableJoint.breakTorque;
                        _name[7] = _breakForce.ToString ();
                        _name[8] = ", ";
                        _anchor = configurableJoint.anchor;
                        _name[9] = _anchor.ToString ();
                        _name[10] = ", ";
                        _anchor = configurableJoint.connectedAnchor;
                        _name[11] = _anchor.ToString ();
                        _name[12] = ", ";

                        string [] strArrays = _name;

                        if (configurableJoint.connectedBody == null)
                        {
                            str1 = "--";
                        }
                        else
                        {
                            _anchor = activeVessel [i].transform.position - configurableJoint.connectedBody.position;

                            str1 = _anchor.ToString ();
                        }

                        strArrays [13] = str1;

                        _name [14] = ", ";
                        _breakForce = configurableJoint.linearLimitSpring.damper;
                        _name [15] = _breakForce.ToString ("F2");
                        _name [16] = ", ";
                        _breakForce = configurableJoint.linearLimitSpring.spring;
                        _name [17] = _breakForce.ToString ("F2");

                        Debug.Log (string.Concat (_name));
                    }
                }

                PartJoint [] partJointArray = activeVessel[i].gameObject.GetComponents<PartJoint>();

                if (partJointArray != null)
                {
                    for (j = 0; j < (int)partJointArray.Length; j++)
                    {
                        PartJoint partJoint = partJointArray [j];

                        if ((partJoint.Host != null ? true : !(partJoint.Target == null)))
                        {
                            _name = new string [] { "[PF]: <PartJoint>, ", partJoint.Host.name, ", ", null, null, null, null, null, null, null, null };

                            _name[3] = (partJoint.Target == null ? "<none>" : partJoint.Target.name);

                            string [] strArrays1 = _name;

                            if (partJoint.Joint == null)
                            {
                                int count = partJoint.joints.Count;

                                str = string.Concat ("<no single joint> (", count.ToString (), ")");
                            }
                            else
                            {
                                _breakForce = partJoint.Joint.breakForce;

                                string str2 = _breakForce.ToString ();

                                _breakForce = partJoint.Joint.breakTorque;

                                str = string.Concat (", ", str2, ", ", _breakForce.ToString ());
                            }

                            strArrays1 [4] = str;

                            _name [5] = ", ";
                            _breakForce = partJoint.stiffness;
                            _name [6] = _breakForce.ToString ("F2");
                            _name [7] = ", ";
                            _anchor = partJoint.HostAnchor;
                            _name [8] = _anchor.ToString ();
                            _name [9] = ", ";
                            _anchor = partJoint.TgtAnchor;
                            _name [10] = _anchor.ToString ();

                            Debug.Log (string.Concat (_name));
                        }
                        else
                        {
                            Debug.Log ("[PF]: <PartJoint>, <none>, <none>");
                        }

                        if (partJoint.Target)
                        {
                            AttachNode attachNode = activeVessel [i].FindAttachNodeByPart (partJoint.Target);

                            if (attachNode != null)
                            {
                                object [] objArray = { "[PF]: <AttachNode>, ", partJoint.Host.name, ", ", partJoint.Target.name, ", ", attachNode.breakingForce.ToString(), ", ", attachNode.breakingTorque.ToString(), ", ", attachNode.contactArea.ToString("F2"), ", ", attachNode.attachMethod, ", ", attachNode.rigid.ToString (), ", ", attachNode.radius.ToString ("F2") };

                                Debug.Log (string.Concat (objArray));

                                AttachNode attachNode1 = attachNode.FindOpposingNode ();

                                if ((attachNode1 == null ? false : attachNode1.owner != null))
                                {
                                    objArray = new object [] { "[PF]: <Opposing AttachNode>, ", attachNode1.owner.name, ", ", (attachNode1.attachedPart != null ? attachNode1.attachedPart.name : "<none>"), ", ", attachNode1.breakingForce.ToString(), ", ", attachNode1.breakingTorque.ToString(), ", ", attachNode1.contactArea.ToString("F2"), ", ", attachNode1.attachMethod, ", ", attachNode1.rigid.ToString (), ", ", attachNode1.radius.ToString ("F2") };

                                    Debug.Log (string.Concat (objArray));
                                }
                            }
                        }
                    }
                }

                FixedJoint [] fixedJointArray = activeVessel [i].gameObject.GetComponents<FixedJoint>();

                if (fixedJointArray != null)
                {
                    for (j = 0; j < (int)fixedJointArray.Length; j++)
                    {
                        FixedJoint fixedJoint = fixedJointArray [j];

                        _name = new string [] { "[PF]: <FixedJoint>, ", fixedJoint.name, ", ", null, null, null, null, null, null, null, null, null };

                        _name [3] = (fixedJoint.connectedBody == null ? "<none>" : fixedJoint.connectedBody.name);
                        _name [4] = ", ";
                        _breakForce = fixedJoint.breakForce;
                        _name [5] = _breakForce.ToString ();
                        _name [6] = ", ";
                        _breakForce = fixedJoint.breakTorque;
                        _name [7] = _breakForce.ToString ();
                        _name [8] = ", ";
                        _anchor = fixedJoint.anchor;
                        _name [9] = _anchor.ToString ();
                        _name [10] = ", ";
                        _anchor = fixedJoint.connectedAnchor;
                        _name [11] = _anchor.ToString ();

                        Debug.Log (string.Concat (_name));
                    }
                }
            }
        }

        LineRenderer makeLineRenderer (string objectName, Color color, float wd)
        {
            var gameObjectLine = new GameObject (objectName);

            gameObjectLine.transform.parent = part.transform;

            gameObjectLine.transform.localPosition = Vector3.zero;
            gameObjectLine.transform.localRotation = Quaternion.identity;

            LineRenderer lineRenderer = gameObjectLine.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = true;
            lineRenderer.material = new Material (Shader.Find ("Particles/Additive"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = wd;
            lineRenderer.endWidth = wd;
            lineRenderer.positionCount = 0;

            return lineRenderer;
        }

        public void OnDestroy ()
        {
            viewJoints = false;

            ClearJointLines ();

            GameEvents.onGameSceneLoadRequested.Remove (new EventData<GameScenes>.OnEvent (OnGameSceneLoadRequested));
            GameEvents.onVesselWasModified.Remove (new EventData<Vessel>.OnEvent (OnVesselModified));
            GameEvents.onPartJointBreak.Remove (new EventData<PartJoint, float>.OnEvent (OnPartJointBreak));
            GameEvents.onVesselGoOffRails.Remove (new EventData<Vessel>.OnEvent (OnVesselGoOffRails));
        }

        void OnGameSceneLoadRequested (GameScenes scene)
        {
            if ((scene == GameScenes.FLIGHT ? false : viewJoints))
            {
                viewJoints = false;

                ClearJointLines ();
            }
        }

        void OnPartJointBreak (PartJoint pj, float value)
        {
            Part host;
            int i;
            bool flag;

            if (pj.Host != part)
            {
                if (pj.Target == part)
                {
                    host = pj.Host;

                    if (nodeParts.Contains(host))
                    {
                        if (bottomNodePart != null)
                        {
                            RemoveJoints (host, bottomNodePart);
                        }

                        RemoveJoints (host, part);

                        flag = nodeParts.Remove (host);
                    }
                    else if (host == bottomNodePart)
                    {
                        for (i = 0; i < nodeParts.Count; i++)
                        {
                            RemoveJoints (nodeParts [i], bottomNodePart);
                        }

                        RemoveJoints (bottomNodePart, part);
                    }

                    return;
                }

                return;
            }

            host = pj.Target;

            if (nodeParts.Contains(host))
            {
                if (bottomNodePart != null)
                {
                    RemoveJoints (host, bottomNodePart);
                }

                RemoveJoints (host, part);

                flag = nodeParts.Remove (host);
            }
            else if (host == bottomNodePart)
            {
                for (i = 0; i < nodeParts.Count; i++)
                {
                    RemoveJoints (nodeParts [i], bottomNodePart);
                }

                RemoveJoints (bottomNodePart, part);
            }
        }

        public override void OnStart (PartModule.StartState state)
        {
            base.OnStart (state);

            var _uiControlFlight = (UI_Toggle) Fields["viewJoints"].uiControlFlight;

            _uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine (_uiControlFlight.onFieldChanged, new Callback<BaseField, object>(UIviewJoints_changed));

            GameEvents.onGameSceneLoadRequested.Add (new EventData<GameScenes>.OnEvent (OnGameSceneLoadRequested));
            GameEvents.onVesselWasModified.Add (new EventData<Vessel>.OnEvent (OnVesselModified));
            GameEvents.onPartJointBreak.Add (new EventData<PartJoint, float>.OnEvent (OnPartJointBreak));
            GameEvents.onVesselGoOffRails.Add (new EventData<Vessel>.OnEvent (OnVesselGoOffRails));
        }

        void OnVesselGoOffRails (Vessel v)
        {
            if ((v == null ? true : this == null))
            {
            }
        }

        void OnVesselModified (Vessel v)
        {
            if (v == vessel)
            {
                if (viewJoints)
                {
                    ViewJoints ();
                }
            }
        }

        void RemoveJoints (Part p, Part pp)
        {
            if ((p == null || p.Rigidbody == null || pp == null ? false : !(pp.Rigidbody == null)))
            {
                ConfigurableJoint [] components = p.gameObject.GetComponents<ConfigurableJoint>();

                for (int i = 0; i < (int)components.Length; i++)
                {
                    ConfigurableJoint configurableJoint = components [i];

                    if (configurableJoint.connectedBody == pp.Rigidbody)
                    {
                        try
                        {
                            UnityEngine.Object.Destroy (configurableJoint);
                        }
                        catch (Exception exception1)
                        {
                            Exception exception = exception1;

                            string [] str = { "[PF]: RemoveJoint Anomaly (", p.ToString(), ", ", pp.ToString(), "): ", exception.Message };

                            Debug.Log (string.Concat (str));
                        }
                    }
                }
            }
        }

        void UIviewJoints_changed (BaseField bf, object obj)
        {
            if (viewJoints)
            {
                ListJoints ();

                ViewJoints ();
            }
            else
            {
                ClearJointLines();
            }
        }

        public void ViewJoints ()
        {
            ClearJointLines ();

            List<Part> activeVessel = FlightGlobals.ActiveVessel.parts;

            for (int i = 0; i < activeVessel.Count; i++)
            {
                ConfigurableJoint [] components = activeVessel [i].gameObject.GetComponents<ConfigurableJoint>();

                if (components != null)
                {
                    for (int j = 0; j < (int)components.Length; j++)
                    {
                        ConfigurableJoint configurableJoint = components [j];

                        if (configurableJoint.connectedBody != null)
                        {
                            var vector3 = new Vector3 (0f, 5f, 0f);
                            var vector31 = new Vector3 (0.25f, 0f, 0f);

                            Vector3 _position = activeVessel [i].transform.position + vector3;
                            Vector3 _position1 = configurableJoint.connectedBody.position + vector3;
                            Vector3 _position2 = (activeVessel [i].transform.position + (activeVessel [i].transform.rotation * configurableJoint.anchor)) + vector3;

                            Vector3 vector32 = (configurableJoint.connectedBody.position + (configurableJoint.connectedBody.rotation * configurableJoint.connectedAnchor)) + vector3;

                            jointLines.Add (JointLine (_position, _position1, Color.blue, w1));
                            jointLines.Add (JointLine (_position2, vector32 + vector31, Color.yellow, w2));
                            jointLines.Add (JointLine (_position, _position2, Color.gray, 0.03f));
                            jointLines.Add (JointLine (_position1, vector32, Color.gray, 0.03f));
                        }
                    }
                }
            }
        }
    }
}
