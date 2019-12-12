//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Keramzit
{
    public class ProceduralFairingBase : PartModule
    {
        [KSPField] public float outlineWidth = 0.05f;
        [KSPField] public int outlineSlices = 12;
        [KSPField] public Vector4 outlineColor = new Vector4 (0, 0, 0.2f, 1);
        [KSPField] public float verticalStep = 0.1f;
        [KSPField] public float baseSize = 1.25f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Extra radius")]
        [UI_FloatRange (minValue = -1, maxValue = 2, stepIncrement = 0.01f)]
        public float extraRadius;

        [KSPField] public int circleSegments = 24;

        [KSPField] public float sideThickness = 0.05f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Fairing Auto-struts")]
        [UI_Toggle (disabledText = "Off", enabledText = "On")]
        public bool autoStrutSides = true;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Fairing Auto-shape")]
        [UI_Toggle (disabledText = "Off", enabledText = "On")]
        public bool autoShape = true;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Max. size", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0.1f, maxValue = 5, incrementLarge = 1.25f, incrementSmall = 0.125f, incrementSlide = 0.001f)]
        public float manualMaxSize = 0.625f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Cyl. start", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0, maxValue = 50, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float manualCylStart = 0;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Cyl. end", guiFormat = "S4", guiUnits = "m")]
        [UI_FloatEdit (sigFigs = 3, unit = "m", minValue = 0, maxValue = 50, incrementLarge = 1.0f, incrementSmall = 0.1f, incrementSlide = 0.001f)]
        public float manualCylEnd = 1;

        bool limitsSet;

        [KSPField] public float diameterStepLarge = 1.25f;
        [KSPField] public float diameterStepSmall = 0.125f;

        [KSPField] public float heightStepLarge = 1.0f;
        [KSPField] public float heightStepSmall = 0.1f;

        public float updateDelay;
        public bool needShapeUpdate = true;

        Part topBasePart;

        LineRenderer line;

        readonly List<LineRenderer> outline = new List<LineRenderer>();

        List<ConfigurableJoint> joints = new List<ConfigurableJoint>();

        public override string GetInfo ()
        {
            const string infoString = "Attach side fairings and they will be shaped for your attached payload.\n" + "Remember to enable the decoupler if you need one.";

            return infoString;
        }

        public override void OnStart (StartState state)
        {
            limitsSet = false;

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            PFUtils.hideDragStuff (part);

            GameEvents.onEditorShipModified.Add (new EventData<ShipConstruct>.OnEvent (onEditorVesselModified));

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (line)
                {
                    line.transform.Rotate (0, 90, 0);
                }

                DestroyAllLineRenderers ();

                destroyOutline ();

                for (int i = 0; i < outlineSlices; ++i)
                {
                    var r = makeLineRenderer ("fairing outline", outlineColor, outlineWidth);

                    outline.Add (r);

                    r.transform.Rotate (0, i * 360f / outlineSlices, 0);
                }

                ShowHideInterstageNodes ();

                recalcShape ();

                updateDelay = 0.1f;

                needShapeUpdate = true;
            }
            else
            {
                topBasePart = null;

                var adapter = part.GetComponent<ProceduralFairingAdapter>();

                if (adapter)
                {
                    topBasePart = adapter.getTopPart ();
                }
                else
                {
                    var scan = scanPayload ();

                    if (scan.targets.Count > 0)
                    {
                        topBasePart = scan.targets [0];
                    }
                }
            }

            SetUIChangedCallBacks ();

            OnToggleAutoshapeUI ();
        }

        void SetUIChangedCallBacks ()
        {
            ((UI_Toggle) Fields["autoShape"].uiControlEditor).onFieldChanged += OnChangeAutoshapeUI;

            ((UI_FloatEdit) Fields["manualMaxSize"].uiControlEditor).onFieldChanged += OnChangeShapeUI;
            ((UI_FloatEdit) Fields["manualCylStart"].uiControlEditor).onFieldChanged += OnChangeShapeUI;
            ((UI_FloatEdit) Fields["manualCylEnd"].uiControlEditor).onFieldChanged += OnChangeShapeUI;
        }

        void OnChangeAutoshapeUI (BaseField bf, object obj)
        {
            OnToggleAutoshapeUI ();

            needShapeUpdate = true;
        }

        void OnToggleAutoshapeUI ()
        {
            Fields["manualMaxSize"].guiActiveEditor  = !autoShape;
            Fields["manualCylStart"].guiActiveEditor = !autoShape;
            Fields["manualCylEnd"].guiActiveEditor   = !autoShape;

            PFUtils.refreshPartWindow ();
        }

        void OnChangeShapeUI (BaseField bf, object obj)
        {
            needShapeUpdate = true;
        }

        void onEditorVesselModified (ShipConstruct ship)
        {
            ShowHideInterstageNodes ();

            needShapeUpdate = true;
        }

        public void ShowHideInterstageNodes ()
        {
            var nnt = part.GetComponent<KzNodeNumberTweaker>();

            if (nnt)
            {
                var nodes = part.FindAttachNodes ("interstage");

                if (nodes == null)
                {
                    return;
                }

                if (nnt.showInterstageNodes)
                {
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes [i];

                        if (node.attachedPart == null)
                        {
                            node.position.x = 0;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes [i];

                        if (node.attachedPart == null)
                        {
                            node.position.x = 10000;
                        }
                    }
                }
            }
        }

        public void removeJoints ()
        {
            while (joints.Count > 0)
            {
                int i = joints.Count - 1;

                var joint = joints [i]; joints.RemoveAt (i);

                UnityEngine.Object.Destroy (joint);
            }
        }

        public void OnPartPack ()
        {
            removeJoints ();
        }

        ConfigurableJoint addStrut (Part p, Part pp)
        {
            if (p == pp)
            {
                return null;
            }

            var rb = pp.Rigidbody;

            if (rb == null || rb == p.Rigidbody)
            {
                return null;
            }

            var joint = p.gameObject.AddComponent<ConfigurableJoint>();

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.projectionDistance = 0.1f;
            joint.projectionAngle = 5;
            joint.breakForce = p.breakingForce;
            joint.breakTorque = p.breakingTorque;
            joint.connectedBody = rb;

            joints.Add (joint);

            return joint;
        }

        IEnumerator<YieldInstruction> createAutoStruts (List<Part> shieldedParts)
        {
            while (!FlightGlobals.ready || vessel.packed || !vessel.loaded)
            {
                yield return new WaitForFixedUpdate ();
            }

            var nnt = part.GetComponent<KzNodeNumberTweaker>();

            var attached = part.FindAttachNodes ("connect");

            for (int i = 0; i < nnt.numNodes; ++i)
            {
                var p = attached [i].attachedPart;

                if (p == null || p.Rigidbody == null)
                {
                    continue;
                }

                var pp = attached [i > 0 ? i - 1 : nnt.numNodes - 1].attachedPart;

                if (pp == null)
                {
                    continue;
                }

                addStrut (p, pp);

                if (topBasePart != null)
                {
                    addStrut (p, topBasePart);
                }
            }
        }

        public void onShieldingDisabled (List<Part> shieldedParts)
        {
            removeJoints ();
        }

        public void onShieldingEnabled (List<Part> shieldedParts)
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            if (autoStrutSides)
            {
                StartCoroutine(createAutoStruts (shieldedParts));
            }
        }

        public virtual void FixedUpdate ()
        {
            if (!limitsSet && PFUtils.canCheckTech ())
            {
                limitsSet = true;

                float minSize = PFUtils.getTechMinValue ("PROCFAIRINGS_MINDIAMETER", 0.25f);
                float maxSize = PFUtils.getTechMaxValue ("PROCFAIRINGS_MAXDIAMETER", 30);

                PFUtils.setFieldRange (Fields["manualMaxSize"], minSize, maxSize * 2);

                ((UI_FloatEdit) Fields["manualMaxSize"].uiControlEditor).incrementLarge = diameterStepLarge;
                ((UI_FloatEdit) Fields["manualMaxSize"].uiControlEditor).incrementSmall = diameterStepSmall;

                ((UI_FloatEdit) Fields["manualCylStart"].uiControlEditor).incrementLarge = heightStepLarge;
                ((UI_FloatEdit) Fields["manualCylStart"].uiControlEditor).incrementSmall = heightStepSmall;
                ((UI_FloatEdit) Fields["manualCylEnd"].uiControlEditor).incrementLarge = heightStepLarge;
                ((UI_FloatEdit) Fields["manualCylEnd"].uiControlEditor).incrementSmall = heightStepSmall;
            }

            if (!part.packed && topBasePart != null)
            {
                var adapter = part.GetComponent<ProceduralFairingAdapter>();

                if (adapter)
                {
                    topBasePart = adapter.getTopPart ();

                    if (topBasePart == null)
                    {
                        removeJoints ();
                    }
                }
            }
        }

        LineRenderer makeLineRenderer (string gameObjectName, Color color, float wd)
        {
            var o = new GameObject (gameObjectName);

            o.transform.parent = part.transform;
            o.transform.localPosition = Vector3.zero;
            o.transform.localRotation = Quaternion.identity;

            var lineRenderer = o.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = false;
            lineRenderer.material = new Material (Shader.Find ("Particles/Additive"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = wd;
            lineRenderer.endWidth = wd;
            lineRenderer.positionCount = 0;

            return lineRenderer;
        }

        void destroyOutline ()
        {
            for (int i = 0; i < outline.Count; i++)
            {
                Destroy (outline [i].gameObject);
            }

            outline.Clear ();
        }

        /// <summary>
        /// Fix for the blue ghost lines showing invalid outlines when cloning or symmetry-placing fairing bases in the editor.
        /// Find any already assigned (copied) LineRenderers and delete them.
        /// </summary>

        void DestroyAllLineRenderers ()
        {
            LineRenderer [] lr = FindObjectsOfType<LineRenderer>();

            if (lr != null)
            {
                for (int i = 0; i < lr.Length; i++)
                {
                    Transform _transform = lr[i].transform;

                    if (_transform != null)
                    {
                        Transform _parent = _transform.parent;

                        if (_parent != null)
                        {
                            GameObject _gameObject = _parent.gameObject;

                            if (_gameObject)
                            {
                                if ((_gameObject.Equals (this) ? true : _gameObject.Equals (gameObject)))
                                {
                                    GameObjectExtension.DestroyGameObject (lr [i].gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnDestroy ()
        {
            GameEvents.onEditorShipModified.Remove (new EventData<ShipConstruct>.OnEvent (onEditorVesselModified));

            if (line)
            {
                Destroy (line.gameObject);

                line = null;
            }

            DestroyAllLineRenderers ();

            destroyOutline ();
        }

        public void Update ()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (updateDelay > 0)
                {
                    updateDelay -= Time.deltaTime;
                }
                else
                {
                    if (needShapeUpdate)
                    {
                        needShapeUpdate = false;
                        updateDelay = 0.1f;

                        recalcShape ();
                    }
                }
            }
        }

        static public Vector3 [] buildFairingShape (float baseRad, float maxRad, float cylStart, float cylEnd, float noseHeightRatio, Vector4 baseConeShape, Vector4 noseConeShape, int baseConeSegments, int noseConeSegments, Vector4 vertMapping, float mappingScaleY)
        {
            float baseConeRad = maxRad - baseRad;
            float tip = maxRad * noseHeightRatio;

            var baseSlope = new BezierSlope (baseConeShape);
            var noseSlope = new BezierSlope (noseConeShape);

            float baseV0 = vertMapping.x / mappingScaleY;
            float baseV1 = vertMapping.y / mappingScaleY;
            float noseV0 = vertMapping.z / mappingScaleY;
            float noseV1 = vertMapping.w / mappingScaleY;

            var shape = new Vector3 [1 + (cylStart.Equals (0) ? 0 : baseConeSegments) + 1 + noseConeSegments];

            int vi = 0;

            if (!cylStart.Equals (0))
            {
                for (int i = 0; i <= baseConeSegments; ++i, ++vi)
                {
                    float t = (float) i / baseConeSegments;

                    var p = baseSlope.interp (t);

                    shape [vi] = new Vector3 (p.x * baseConeRad + baseRad, p.y * cylStart, Mathf.Lerp (baseV0, baseV1, t));
                }
            }
            else
            {
                shape [vi++] = new Vector3 (baseRad, 0, baseV1);
            }

            for (int i = 0; i <= noseConeSegments; ++i, ++vi)
            {
                float t = (float) i / noseConeSegments;

                var p = noseSlope.interp (1 - t);

                shape [vi] = new Vector3 (p.x * maxRad, (1 - p.y) * tip + cylEnd, Mathf.Lerp (noseV0, noseV1, t));
            }

            return shape;
        }

        static public Vector3 [] buildInlineFairingShape (float baseRad, float maxRad, float topRad, float cylStart, float cylEnd, float top, Vector4 baseConeShape, int baseConeSegments, Vector4 vertMapping, float mappingScaleY)
        {
            float baseConeRad = maxRad - baseRad;
            float topConeRad = maxRad - topRad;

            var baseSlope = new BezierSlope (baseConeShape);

            float baseV0 = vertMapping.x / mappingScaleY;
            float baseV1 = vertMapping.y / mappingScaleY;
            float noseV0 = vertMapping.z / mappingScaleY;

            var shape = new Vector3 [2 + (cylStart.Equals (0) ? 0 : baseConeSegments + 1) + (cylEnd.Equals (top) ? 0 : baseConeSegments + 1)];

            int vi = 0;

            if (!cylStart.Equals(0))
            {
                for (int i = 0; i <= baseConeSegments; ++i, ++vi)
                {
                    float t = (float) i / baseConeSegments;

                    var p = baseSlope.interp (t);

                    shape [vi] = new Vector3 (p.x * baseConeRad + baseRad, p.y * cylStart, Mathf.Lerp (baseV0, baseV1, t));
                }
            }

            shape [vi++] = new Vector3 (maxRad, cylStart, baseV1);
            shape [vi++] = new Vector3 (maxRad, cylEnd, noseV0);

            if (!cylEnd.Equals (top))
            {
                for (int i = 0; i <= baseConeSegments; ++i, ++vi)
                {
                    float t = (float) i / baseConeSegments;

                    var p = baseSlope.interp (1 - t);

                    shape [vi] = new Vector3 (p.x * topConeRad + topRad, Mathf.Lerp (top, cylEnd, p.y), Mathf.Lerp (baseV1, baseV0, t));
                }
            }

            return shape;
        }

        PayloadScan scanPayload ()
        {
            //  Scan the payload and build it's profile.

            var scan = new PayloadScan (part, verticalStep, extraRadius);

            AttachNode node = part.FindAttachNode ("top");

            if (node != null)
            {
                scan.ofs = node.position.y;

                if (node.attachedPart != null)
                {
                    scan.addPart (node.attachedPart, part);
                }
            }

            AttachNode [] nodes = part.FindAttachNodes ("interstage");

            if (nodes != null)
            {
                for (int j = 0; j < nodes.Length; j++)
                {
                    node = nodes [j];

                    if (node != null)
                    {
                        if (node.attachedPart != null)
                        {
                            scan.addPart (node.attachedPart, part);
                        }
                    }
                }
            }

            for (int i = 0; i < scan.payload.Count; ++i)
            {
                var cp = scan.payload [i];

                //  Add any connected payload parts.

                scan.addPart (cp.parent, cp);

                for (int j = 0; j < cp.children.Count; j++)
                {
                    scan.addPart(cp.children[j], cp);
                }

                //  Scan for the part colliders.

                var colls = cp.FindModelComponents<Collider>();

                for (int j = 0; j < colls.Count; j++)
                {
                    var coll = colls [j];

                    //  Skip ladders etc...

                    if (coll.tag != "Untagged")
                    {
                        continue;
                    }

                    scan.addPayload (coll);
                }
            }

            return scan;
        }

        AttachNode HasNodeComponent<type> (AttachNode [] nodes)
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    var partAttached = nodes [i].attachedPart;

                    if (partAttached == null)
                    {
                        continue;
                    }

                    var comp = partAttached.GetComponent<type>();

                    if (!comp.Equals (null))
                    {
                        return nodes [i];
                    }
                }
            }

            return null;
        }

        public void recalcShape ()
        {
            var scan = scanPayload ();

            //  Check for reversed bases (inline fairings).

            float topY = 0;
            float topRad = 0;

            AttachNode topSideNode = null;

            bool isInline = false;

            var adapter = part.GetComponent<ProceduralFairingAdapter>();

            if (adapter)
            {
                isInline = true;

                topY = adapter.height + adapter.extraHeight;

                if (topY < scan.ofs)
                {
                    topY = scan.ofs;
                }

                topRad = adapter.topRadius;
            }
            else if (scan.targets.Count > 0)
            {
                isInline = true;

                var topBase = scan.targets [0].GetComponent<ProceduralFairingBase>();

                topY = scan.w2l.MultiplyPoint3x4 (topBase.part.transform.position).y;

                if (topY < scan.ofs)
                {
                    topY = scan.ofs;
                }

                topSideNode = HasNodeComponent<ProceduralFairingSide>(topBase.part.FindAttachNodes ("connect"));

                topRad = topBase.baseSize * 0.5f;
            }

            //  No payload case.

            if (scan.profile.Count <= 0)
            {
                scan.profile.Add (extraRadius);
            }

            //  Fill profile outline (for debugging).

            if (line)
            {
                line.positionCount = scan.profile.Count * 2 + 2;

                float prevRad = 0;

                int hi = 0;

                for (int i = 0; i < scan.profile.Count; i++)
                {
                    var r = scan.profile [i];

                    line.SetPosition (hi * 2, new Vector3 (prevRad, hi * verticalStep + scan.ofs, 0));
                    line.SetPosition (hi * 2 + 1, new Vector3 (r, hi * verticalStep + scan.ofs, 0));

                    hi++; prevRad = r;
                }

                line.SetPosition (hi * 2, new Vector3 (prevRad, hi * verticalStep + scan.ofs, 0));
                line.SetPosition (hi * 2 + 1, new Vector3 (0, hi * verticalStep + scan.ofs, 0));
            }

            //  Check for attached side parts.

            var attached = part.FindAttachNodes ("connect");

            //  Get the number of available fairing attachment nodes from NodeNumberTweaker.

            var nnt = part.GetComponent<KzNodeNumberTweaker>();

            int numSideParts = nnt.numNodes;

            var sideNode = HasNodeComponent<ProceduralFairingSide>(attached);

            var baseConeShape = new Vector4 (0, 0, 0, 0);
            var noseConeShape = new Vector4 (0, 0, 0, 0);
            var mappingScale = new Vector2 (1024, 1024);
            var stripMapping = new Vector2 (992, 1024);
            var horMapping = new Vector4 (0, 480, 512, 992);
            var vertMapping = new Vector4 (0, 160, 704, 1024);

            float baseCurveStartX = 0;
            float baseCurveStartY = 0;
            float baseCurveEndX = 0;
            float baseCurveEndY = 0;

            int baseConeSegments = 1;

            float noseCurveStartX = 0;
            float noseCurveStartY = 0;
            float noseCurveEndX = 0;
            float noseCurveEndY = 0;

            int noseConeSegments = 1;
            float noseHeightRatio = 1;
            float minBaseConeAngle = 20;

            float density = 0;

            if (sideNode != null)
            {
                var sf = sideNode.attachedPart.GetComponent<ProceduralFairingSide>();

                mappingScale = sf.mappingScale;
                stripMapping = sf.stripMapping;
                horMapping = sf.horMapping;
                vertMapping = sf.vertMapping;
                noseHeightRatio = sf.noseHeightRatio;
                minBaseConeAngle = sf.minBaseConeAngle;
                baseConeShape = sf.baseConeShape;
                baseCurveStartX = sf.baseCurveStartX;
                baseCurveStartY = sf.baseCurveStartY;
                baseCurveEndX = sf.baseCurveEndX;
                baseCurveEndY = sf.baseCurveEndY;
                baseConeSegments = (int) sf.baseConeSegments;
                noseConeShape = sf.noseConeShape;
                noseCurveStartX = sf.noseCurveStartX;
                noseCurveStartY = sf.noseCurveStartY;
                noseCurveEndX = sf.noseCurveEndX;
                noseCurveEndY = sf.noseCurveEndY;
                noseConeSegments = (int) sf.noseConeSegments;
                density = sf.density;
            }

            //   Compute the fairing shape.

            float baseRad = baseSize * 0.5f;
            float minBaseConeTan = Mathf.Tan (minBaseConeAngle * Mathf.Deg2Rad);

            float cylStart = 0;
            float maxRad;

            int profTop = scan.profile.Count;

            if (isInline)
            {
                profTop = Mathf.CeilToInt ((topY - scan.ofs) / verticalStep);

                if (profTop > scan.profile.Count)
                {
                    profTop = scan.profile.Count;
                }

                maxRad = 0;

                for (int i = 0; i < profTop; ++i)
                {
                    maxRad = Mathf.Max (maxRad, scan.profile [i]);
                }

                maxRad = Mathf.Max (maxRad, topRad);
            }
            else
            {
                maxRad = PFUtils.GetMaxValueFromList (scan.profile);
            }

            if (maxRad > baseRad)
            {
                //  Try to fit the base cone as high as possible.

                cylStart = scan.ofs;

                for (int i = 1; i < scan.profile.Count; ++i)
                {
                    float y = i * verticalStep + scan.ofs;
                    float r0 = baseRad;
                    float k = (maxRad - r0) / y;

                    if (k < minBaseConeTan)
                    {
                        break;
                    }

                    bool ok = true;

                    float r = r0 + k * scan.ofs;

                    for (int j = 0; j < i; ++j, r += k * verticalStep)
                    {
                        if (scan.profile [j] > r)
                        {
                            ok = false;

                            break;
                        }
                    }

                    if (!ok)
                    {
                        break;
                    }

                    cylStart = y;
                }
            }
            else
            {
                //  No base cone, just a cylinder and a nose.

                maxRad = baseRad;
            }

            float cylEnd = scan.profile.Count * verticalStep + scan.ofs;

            if (isInline)
            {
                float r0 = topRad;

                if (profTop > 0 && profTop < scan.profile.Count)
                {
                    r0 = Mathf.Max (r0, scan.profile [profTop - 1]);

                    if (profTop - 2 >= 0) r0 = Mathf.Max (r0, scan.profile [profTop - 2]);
                }

                if (maxRad > r0)
                {
                    if (cylEnd > topY)
                    {
                        cylEnd = topY - verticalStep;
                    }

                    //  Try to fit the top cone as low as possible.

                    for (int i = profTop - 1; i >= 0; --i)
                    {
                        float y = i * verticalStep + scan.ofs;
                        float k = (maxRad - r0) / (y - topY);

                        bool ok = true;

                        float r = maxRad + k * verticalStep;

                        for (int j = i; j < profTop; ++j, r += k * verticalStep)
                        {
                            if (r < r0)
                            {
                                r = r0;
                            }

                            if (scan.profile [j] > r)
                            {
                                ok = false;

                                break;
                            }
                        }

                        if (!ok)
                        {
                            break;
                        }

                        cylEnd = y;
                    }
                }
                else
                {
                    cylEnd = topY;
                }
            }
            else
            {
                //  Try to fit the nose cone as low as possible.

                for (int i = scan.profile.Count - 1; i >= 0; --i)
                {
                    float s = verticalStep / noseHeightRatio;

                    bool ok = true;

                    float r = maxRad - s;

                    for (int j = i; j < scan.profile.Count; ++j, r -= s)
                    {
                        if (scan.profile [j] > r)
                        {
                            ok = false;

                            break;
                        }
                    }

                    if (!ok) break;

                    float y = i * verticalStep + scan.ofs;

                    cylEnd = y;
                }
            }

            if (autoShape)
            {
                manualMaxSize = maxRad * 2;
                manualCylStart = cylStart;
                manualCylEnd = cylEnd;
            }
            else
            {
                maxRad = manualMaxSize * 0.5f;
                cylStart = manualCylStart;
                cylEnd = manualCylEnd;
            }

            if (cylStart > cylEnd)
            {
                cylStart = cylEnd;
            }

            //  Build the fairing shape line.

            Vector3 [] shape;

            if (isInline)
            {
                shape = buildInlineFairingShape (baseRad, maxRad, topRad, cylStart, cylEnd, topY, baseConeShape, baseConeSegments, vertMapping, mappingScale.y);
            }
            else
            {
                shape = buildFairingShape (baseRad, maxRad, cylStart, cylEnd, noseHeightRatio, baseConeShape, noseConeShape, baseConeSegments, noseConeSegments, vertMapping, mappingScale.y);
            }

            if (sideNode == null && topSideNode == null)
            {
                //  No side parts - fill fairing outlines.

                for (int j = 0; j < outline.Count; j++)
                {
                    var lr = outline [j];

                    lr.positionCount = shape.Length;

                    for (int i = 0; i < shape.Length; ++i)
                    {
                        lr.SetPosition (i, new Vector3 (shape [i].x, shape [i].y));
                    }
                }
            }
            else
            {
                for (int j = 0; j < outline.Count; j++)
                {
                    var lr = outline [j];

                    lr.positionCount = 0;
                }
            }

            //  Rebuild the side parts.

            int numSegs = circleSegments / numSideParts;

            if (numSegs < 2)
            {
                numSegs = 2;
            }

            for (int i = 0; i < attached.Length; i++)
            {
                var sn = attached [i];
                var sp = sn.attachedPart;

                if (!sp)
                {
                    continue;
                }

                var sf = sp.GetComponent<ProceduralFairingSide>();

                if (!sf)
                {
                    continue;
                }

                if (sf.shapeLock)
                {
                    continue;
                }

                var mf = sp.FindModelComponent<MeshFilter>("model");

                if (!mf)
                {
                    Debug.LogError ("[PF]: No model in side fairing!", sp);

                    continue;
                }

                var nodePos = sn.position;

                mf.transform.position = part.transform.position;
                mf.transform.rotation = part.transform.rotation;

                float ra = Mathf.Atan2 (-nodePos.z, nodePos.x) * Mathf.Rad2Deg;

                mf.transform.Rotate (0, ra, 0);

                if (sf.meshPos == mf.transform.localPosition
                 && sf.meshRot == mf.transform.localRotation
                 && sf.numSegs == numSegs
                 && sf.numSideParts == numSideParts
                 && sf.baseRad.Equals (baseRad)
                 && sf.maxRad.Equals (maxRad)
                 && sf.cylStart.Equals (cylStart)
                 && sf.cylEnd.Equals (cylEnd)
                 && sf.topRad.Equals (topRad)
                 && sf.inlineHeight.Equals (topY)
                 && sf.sideThickness.Equals (sideThickness)
                 && !sf.baseCurveStartX.Equals (baseCurveStartX)
                 && !sf.baseCurveStartY.Equals (baseCurveStartY)
                 && !sf.baseCurveEndX.Equals (baseCurveEndX)
                 && !sf.baseCurveEndY.Equals (baseCurveEndY)
                 && !sf.baseConeSegments.Equals (baseConeSegments)
                 && !sf.noseCurveStartX.Equals (noseCurveStartX)
                 && !sf.noseCurveStartY.Equals (noseCurveStartY)
                 && !sf.noseCurveEndX.Equals (noseCurveEndX)
                 && !sf.noseCurveEndY.Equals (noseCurveEndY)
                 && !sf.noseConeSegments.Equals (noseConeSegments)
                 && !sf.noseHeightRatio.Equals (noseHeightRatio)
                 && !sf.density.Equals (density))
                {
                    continue;
                }

                sf.meshPos = mf.transform.localPosition;
                sf.meshRot = mf.transform.localRotation;
                sf.numSegs = numSegs;
                sf.numSideParts = numSideParts;
                sf.baseRad = baseRad;
                sf.maxRad = maxRad;
                sf.cylStart = cylStart;
                sf.cylEnd = cylEnd;
                sf.topRad = topRad;
                sf.inlineHeight = topY;
                sf.sideThickness = sideThickness;
                sf.baseCurveStartX = baseCurveStartX;
                sf.baseCurveStartY = baseCurveStartY;
                sf.baseCurveEndX = baseCurveEndX;
                sf.baseCurveEndY = baseCurveEndY;
                sf.baseConeSegments = baseConeSegments;
                sf.noseCurveStartX = noseCurveStartX;
                sf.noseCurveStartY = noseCurveStartY;
                sf.noseCurveEndX = noseCurveEndX;
                sf.noseCurveEndY = noseCurveEndY;
                sf.noseConeSegments = noseConeSegments;
                sf.noseHeightRatio = noseHeightRatio;
                sf.density = density;

                sf.rebuildMesh ();
            }

            var shielding = part.GetComponent<KzFairingBaseShielding>();

            if (shielding)
            {
                shielding.reset ();
            }
        }
    }
}
