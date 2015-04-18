// Procedural Fairings plug-in by Alexey Volynskov
// Licensed under CC BY 3.0 terms: http://creativecommons.org/licenses/by/3.0/
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;


namespace Keramzit {


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


public class KzFairingBaseShielding : PartModule, IAirstreamShield
{
  List<Part> shieldedParts;

  ProceduralFairingSide sideFairing;
  float boundCylY0, boundCylY1, boundCylRad;
  Vector3 lookupCenter;
  float lookupRad;
  Vector3[] shape;


  public bool ClosedAndLocked() { return true; }
  public Vessel GetVessel() { return vessel; }
  public Part GetPart() { return part; }


  public override void OnAwake()
  {
    shieldedParts=new List<Part>();
  }


  public override void OnStart(StartState state)
  {
    if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) return;

    reset();

    GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
    GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(onVesselModified));
    GameEvents.onVesselGoOffRails.Add(new EventData<Vessel>.OnEvent(OnVesselUnpack));
    GameEvents.onVesselGoOnRails.Add(new EventData<Vessel>.OnEvent(OnVesselPack));
    GameEvents.onPartDie.Add(new EventData<Part>.OnEvent(OnPartDestroyed));
  }


  void OnDestroy()
  {
    GameEvents.onEditorShipModified.Remove(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
    GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(onVesselModified));
    GameEvents.onVesselGoOffRails.Remove(new EventData<Vessel>.OnEvent(OnVesselUnpack));
    GameEvents.onVesselGoOnRails.Remove(new EventData<Vessel>.OnEvent(OnVesselPack));
    GameEvents.onPartDie.Remove(new EventData<Part>.OnEvent(OnPartDestroyed));
  }


  void reset()
  {
    getFairingParams();
    bool shield = (HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsFlight && !vessel.packed));
    if (shield) enableShielding();
  }


  AttachNode[] getFairingParams()
  {
    // check attached side parts and get params
    var attached=part.findAttachNodes("connect");
    ProceduralFairingSide sf=null;

    for (int i=0; i<attached.Length; ++i)
    {
      var n=attached[i];
      if (!n.attachedPart) { sf=null; break; }
      sf=n.attachedPart.GetComponent<ProceduralFairingSide>();
      if (!sf) break;
    }

    sideFairing=sf;

    if (!sf)
    {
      shape=null;
      boundCylY0=boundCylY1=boundCylRad=0;
      lookupCenter=Vector3.zero;
      lookupRad=0;
      return null;
    }

    // get shape polyline
    if (sf.inlineHeight<=0)
      shape=ProceduralFairingBase.buildFairingShape(
        sf.baseRad, sf.maxRad, sf.cylStart, sf.cylEnd, sf.noseHeightRatio,
        sf.baseConeShape, sf.noseConeShape, sf.baseConeSegments, sf.noseConeSegments,
        sf.vertMapping, sf.mappingScale.y);
    else
      shape=ProceduralFairingBase.buildInlineFairingShape(
        sf.baseRad, sf.maxRad, sf.topRad, sf.cylStart, sf.cylEnd, sf.inlineHeight,
        sf.baseConeShape, sf.baseConeSegments,
        sf.vertMapping, sf.mappingScale.y);

    // offset shape by thickness
    for (int i=0; i<shape.Length; ++i)
    {
      if (i==0 || i==shape.Length-1)
        shape[i]+=new Vector3(sf.sideThickness, 0, 0);
      else
      {
        Vector2 n=shape[i+1]-shape[i-1];
        n.Set(n.y, -n.x);
        n.Normalize();
        shape[i]+=new Vector3(n.x, n.y, 0)*sf.sideThickness;
      }
    }

    // compute bounds
    float y0, y1, mr;
    y0=y1=shape[0].y;
    mr=shape[0].x;

    for (int i=0; i<shape.Length; ++i)
    {
      var p=shape[i];
      if (p.x>mr) mr=p.x;
      if (p.y<y0) y0=p.y;
      else if (p.y>y1) y1=p.y;
    }

    boundCylY0=y0;
    boundCylY1=y1;
    boundCylRad=mr;

    lookupCenter=new Vector3(0, (y0+y1)*0.5f, 0);
    lookupRad=new Vector2(mr, (y1-y0)*0.5f).magnitude;

    return attached;
  }


  void enableShielding()
  {
    disableShielding();

    var attached=getFairingParams();
    if (!sideFairing) return;

    // get all parts in range
    var parts=new List<Part>();
    var colliders=Physics.OverlapSphere(part.transform.TransformPoint(lookupCenter), lookupRad, 1);
    for (int i=colliders.Length-1; i>=0; --i)
    {
      var p=colliders[i].gameObject.GetComponentUpwards<Part>();
      if (p!=null) parts.AddUnique(p);
    }

    //== check if the top is closed in inline/adapter case

    // filter parts
    float sizeSqr=lookupRad*lookupRad*4;
    float boundCylRadSq=boundCylRad*boundCylRad;

    for (int pi=0; pi<parts.Count; ++pi)
    {
      var pt=parts[pi];

      // check special cases
      if (pt==part) { shieldedParts.Add(pt); continue; }

      bool isSide=false;
      for (int i=0; i<attached.Length; ++i) if (attached[i].attachedPart==pt) { isSide=true; break; }
      if (isSide) continue;

      // check if too big to fit
      var bounds=pt.GetRendererBounds();
      if (PartGeometryUtil.MergeBounds(bounds, pt.partTransform).size.sqrMagnitude>sizeSqr) continue;

      // check if the centroid is within fairing bounds
      var c=part.transform.InverseTransformPoint(PartGeometryUtil.FindBoundsCentroid(bounds, null));

      float y=c.y;
      if (y<boundCylY0 || y>boundCylY1) continue;

      float xsq=new Vector2(c.x, c.z).sqrMagnitude;
      if (xsq>boundCylRadSq) continue;

      // accurate centroid check
      float x=Mathf.Sqrt(xsq);
      bool inside=false;

      for (int i=1; i<shape.Length; ++i)
      {
        var p0=shape[i-1];
        var p1=shape[i];
        if (p0.y>p1.y) { var p=p0; p0=p1; p1=p; }

        if (y<p0.y || y>p1.y) continue;

        float dy=p1.y-p0.y, r;
        if (dy<=1e-6f) r=(p0.x+p1.x)*0.5f;
        else r=(p1.x-p0.x)*(y-p0.y)/dy+p0.x;

        if (x>r) continue;

        inside=true;
        break;
      }

      if (!inside) continue;

      shieldedParts.Add(pt);
    }

    // add shielding
    for (int i=0; i<shieldedParts.Count; ++i)
      shieldedParts[i].AddShield(this);
  }


  void disableShielding()
  {
    if (shieldedParts!=null)
    {
      for (int i=shieldedParts.Count()-1; i>=0; --i)
        if (shieldedParts[i]!=null) shieldedParts[i].RemoveShield(this);
      shieldedParts.Clear();
    }
  }


  void onEditorVesselModified(ShipConstruct ship)
  {
    reset();
  }


  void onVesselModified(Vessel v)
  {
    if (v!=vessel)
    {
      var dp=v.vesselTransform.position-part.transform.TransformPoint(lookupCenter);
      if (dp.sqrMagnitude>lookupRad*lookupRad) return;
    }
    enableShielding();
  }


  void OnVesselUnpack(Vessel v)
  {
    if (v==vessel) enableShielding();
  }


  void OnVesselPack(Vessel v)
  {
    if (v==vessel) disableShielding();
  }


  void OnPartDestroyed(Part p)
  {
    if (p==part) { disableShielding(); return; }

    // check for side fairing parts
    var attached=part.findAttachNodes("connect");
    for (int i=0; i<attached.Length; ++i)
      if (p==attached[i].attachedPart) { disableShielding(); return; }

    // check for top parts in inline/adapter case
    if (p.vessel==vessel && sideFairing && sideFairing.inlineHeight>0)
      enableShielding();
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


} // namespace
