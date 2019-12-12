//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Keramzit
{
    public class KzFairingBaseShielding : PartModule, IAirstreamShield
    {
        List<Part> shieldedParts;

        ProceduralFairingSide sideFairing;

        float boundCylY0, boundCylY1, boundCylRad;
        float lookupRad;

        Vector3 lookupCenter;
        Vector3 [] shape;

        [KSPField (isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Parts shielded")]
        public int numShieldedDisplay;

        bool needReset;

        public bool ClosedAndLocked () { return true; }
        public Vessel GetVessel () { return vessel; }
        public Part GetPart () { return part; }

        public override void OnAwake ()
        {
            shieldedParts = new List<Part>();
        }

        public override void OnStart (StartState state)
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            reset ();

            GameEvents.onVesselWasModified.Add (new EventData<Vessel>.OnEvent (onVesselModified));
            GameEvents.onVesselGoOffRails.Add (new EventData<Vessel>.OnEvent (onVesselUnpack));
            GameEvents.onPartDie.Add (new EventData<Part>.OnEvent (OnPartDestroyed));
        }

        void OnDestroy ()
        {
            GameEvents.onVesselWasModified.Remove (new EventData<Vessel>.OnEvent (onVesselModified));
            GameEvents.onVesselGoOffRails.Remove (new EventData<Vessel>.OnEvent (onVesselUnpack));
            GameEvents.onPartDie.Remove (new EventData<Part>.OnEvent (OnPartDestroyed));
        }

        public void FixedUpdate ()
        {
            if (needReset)
            {
                needReset = false;

                getFairingParams ();

                bool shield = (HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsFlight && !vessel.packed));

                if (shield)
                {
                    enableShielding ();
                }
            }
        }

        public void reset ()
        {
            needReset = true;
        }

        AttachNode [] getFairingParams ()
        {
            var nnt = part.GetComponent<KzNodeNumberTweaker>();

            //  Check for attached side parts and get their parameters.

            var attached = part.FindAttachNodes ("connect");

            ProceduralFairingSide sf = null;

            for (int i = 0; i < nnt.numNodes; ++i)
            {
                var n = attached [i];

                if (!n.attachedPart)
                {
                    sf = null;

                    break;
                }

                sf = n.attachedPart.GetComponent<ProceduralFairingSide>();

                if (!sf)
                {
                    break;
                }
            }

            sideFairing = sf;

            if (!sf)
            {
                shape = null;
                boundCylY0 = boundCylY1 = boundCylRad = 0;
                lookupCenter = Vector3.zero;
                lookupRad = 0;

                return null;
            }

            //  Get the polyline shape.

            if (sf.inlineHeight <= 0)
            {
                shape = ProceduralFairingBase.buildFairingShape (sf.baseRad, sf.maxRad, sf.cylStart, sf.cylEnd, sf.noseHeightRatio, sf.baseConeShape, sf.noseConeShape, (int) sf.baseConeSegments, (int) sf.noseConeSegments, sf.vertMapping, sf.mappingScale.y);
            }
            else
            {
                shape = ProceduralFairingBase.buildInlineFairingShape (sf.baseRad, sf.maxRad, sf.topRad, sf.cylStart, sf.cylEnd, sf.inlineHeight, sf.baseConeShape, (int) sf.baseConeSegments, sf.vertMapping, sf.mappingScale.y);
            }

            //  Offset shape by thickness.

            for (int i = 0; i < shape.Length; ++i)
            {
                if (i == 0 || i == shape.Length - 1)
                {
                    shape [i] += new Vector3 (sf.sideThickness, 0, 0);
                }
                else
                {
                    Vector2 n = shape [i + 1] - shape [i - 1];

                    n.Set (n.y, -n.x);

                    n.Normalize ();

                    shape [i] += new Vector3 (n.x, n.y, 0) * sf.sideThickness;
                }
            }

            //  Compute the bounds.

            float y0, y1, mr;

            y0 = y1 = shape [0].y;
            mr = shape [0].x;

            for (int i = 0; i < shape.Length; ++i)
            {
                var p = shape [i];

                if (p.x > mr)
                {
                    mr = p.x;
                }

                if (p.y < y0)
                {
                    y0 = p.y;
                }
                else if (p.y > y1)
                {
                    y1 = p.y;
                }
            }

            boundCylY0 = y0;
            boundCylY1 = y1;
            boundCylRad = mr;

            lookupCenter = new Vector3 (0, (y0 + y1) * 0.5f, 0);
            lookupRad = new Vector2 (mr, (y1 - y0) * 0.5f).magnitude;

            return attached;
        }

        void enableShielding ()
        {
            disableShielding ();

            var attached = getFairingParams ();

            if (!sideFairing)
            {
                return;
            }

            //  Get all parts in range.

            var parts = new List<Part>();

            var colliders = Physics.OverlapSphere (part.transform.TransformPoint (lookupCenter), lookupRad, 1);

            for (int i = colliders.Length - 1; i >= 0; --i)
            {
                var p = colliders [i].gameObject.GetComponentUpwards<Part>();

                if (p != null)
                {
                    parts.AddUnique (p);
                }
            }

            //  Filter parts.

            float sizeSqr = lookupRad * lookupRad * 4;
            float boundCylRadSq = boundCylRad * boundCylRad;

            bool isInline = (sideFairing.inlineHeight > 0);
            bool topClosed = false;

            Matrix4x4 w2l = Matrix4x4.identity, w2lb = Matrix4x4.identity;

            Bounds topBounds = default (Bounds);

            if (isInline)
            {
                w2l = part.transform.worldToLocalMatrix;
                w2lb = w2l;

                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        w2lb [i, j] = Mathf.Abs (w2lb [i, j]);
                    }
                }

                topBounds = new Bounds (new Vector3 (0, boundCylY1, 0), new Vector3 (sideFairing.topRad * 2, sideFairing.sideThickness, sideFairing.topRad * 2));
            }

            for (int pi = 0; pi < parts.Count; ++pi)
            {
                var pt = parts [pi];

                //  Check special cases.

                if (pt == part)
                {
                    shieldedParts.Add (pt);

                    continue;
                }

                bool isSide = false;

                for (int i = 0; i < attached.Length; ++i)
                {
                    if (attached [i].attachedPart == pt)
                    {
                        isSide = true;

                        break;
                    }
                }

                if (isSide)
                {
                    continue;
                }

                //  Check if the top is closed in the inline case.

                var bounds = pt.GetRendererBounds ();

                var box = PartGeometryUtil.MergeBounds (bounds, pt.transform);

                if (isInline && !topClosed && pt.vessel == vessel)
                {
                    var wb = box; wb.Expand (sideFairing.sideThickness * 4);

                    var b = new Bounds (w2l.MultiplyPoint3x4 (wb.center), w2lb.MultiplyVector (wb.size));

                    if (b.Contains (topBounds.min) && b.Contains (topBounds.max))
                    {
                        topClosed = true;
                    }
                }

                //  Check if the centroid is within the fairing bounds.

                var c = part.transform.InverseTransformPoint (PartGeometryUtil.FindBoundsCentroid (bounds, null));

                float y = c.y;

                if (y < boundCylY0 || y > boundCylY1)
                {
                    continue;
                }

                float xsq = new Vector2 (c.x, c.z).sqrMagnitude;

                if (xsq > boundCylRadSq)
                {
                    continue;
                }

                //  Accurate centroid check.

                float x = Mathf.Sqrt (xsq);

                bool inside = false;

                for (int i = 1; i < shape.Length; ++i)
                {
                    var p0 = shape [i - 1];
                    var p1 = shape [i];

                    if (p0.y > p1.y)
                    {
                        var p = p0;

                        p0 = p1;
                        p1 = p;
                    }

                    if (y < p0.y || y > p1.y)
                    {
                        continue;
                    }

                    float dy = p1.y - p0.y, r;

                    if (dy <= 1e-6f)
                    {
                        r = (p0.x + p1.x) * 0.5f;
                    }
                    else
                    {
                        r = (p1.x - p0.x) * (y - p0.y) / dy + p0.x;
                    }

                    if (x > r)
                    {
                        continue;
                    }

                    inside = true;

                    break;
                }

                if (!inside)
                {
                    continue;
                }

                shieldedParts.Add (pt);
            }

            if (isInline && !topClosed)
            {
                disableShielding ();

                return;
            }

            //  Add shielding.

            for (int i = 0; i < shieldedParts.Count; ++i)
            {
                shieldedParts [i].AddShield (this);
            }

            numShieldedDisplay = shieldedParts.Count;

            var fbase = part.GetComponent<ProceduralFairingBase>();

            if (fbase != null)
            {
                fbase.onShieldingEnabled (shieldedParts);
            }
        }

        void disableShielding ()
        {
            if (shieldedParts != null)
            {
                var fbase = part.GetComponent<ProceduralFairingBase>();

                if (fbase != null)
                {
                    fbase.onShieldingDisabled (shieldedParts);
                }

                for (int i = shieldedParts.Count - 1; i >= 0; --i)
                {
                    if (shieldedParts [i] != null)
                    {
                        shieldedParts [i].RemoveShield (this);
                    }
                }

                shieldedParts.Clear ();
            }

            numShieldedDisplay = 0;
        }

        void onVesselModified (Vessel v)
        {
            if (v != vessel)
            {
                var dp = v.vesselTransform.position - part.transform.TransformPoint (lookupCenter);

                if (dp.sqrMagnitude > lookupRad * lookupRad)
                {
                    return;
                }
            }

            enableShielding ();
        }

        void onVesselUnpack (Vessel v)
        {
            if (v == vessel)
            {
                enableShielding ();
            }
        }

        void onVesselPack (Vessel v)
        {
            if (v == vessel)
            {
                disableShielding ();
            }
        }

        void OnPartDestroyed (Part p)
        {
            var nnt = part.GetComponent<KzNodeNumberTweaker>();

            if (p == part)
            {
                disableShielding ();

                return;
            }

            //  Check for attached side fairing parts.

            var attached = part.FindAttachNodes ("connect");

            for (int i = 0; i < nnt.numNodes; ++i)
            {
                if (p == attached [i].attachedPart)
                {
                    disableShielding ();

                    return;
                }
            }

            //  Check for top parts in the inline/adapter case.

            if (p.vessel == vessel && sideFairing && sideFairing.inlineHeight > 0)
            {
                enableShielding ();
            }
        }

        public void OnPartPack ()
        {
            disableShielding ();
        }
    }
}
