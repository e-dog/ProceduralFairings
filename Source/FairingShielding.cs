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


  void getFairingParams()
  {
    // check attached side parts and get params
    var attached=part.findAttachNodes("connect");
    int numSideParts=attached.Length;

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
      return;
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
  }


  void enableShielding()
  {
    disableShielding();

    getFairingParams();

    //== get all parts in range

    //== filter parts

    //== add shielding
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
      var dp=v.vesselTransform.position - part.transform.TransformPoint(lookupCenter);
      if (dp.sqrMagnitude > lookupRad*lookupRad) return;
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
    if (p==part) disableShielding();
    //== check for side fairing parts
    //== check for top parts in inline/adapter case
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


} // namespace
