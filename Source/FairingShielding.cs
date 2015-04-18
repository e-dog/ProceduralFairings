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
  Vector3 lookupCenter;
  float lookupRad;


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
    //== get fairing params

    bool shield = (HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsFlight && !vessel.packed));
    if (shield) enableShielding();
  }


  void enableShielding()
  {
    disableShielding();

    //==
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
      var dp=v.vesselTransform.position - part.partTransform.TransformPoint(lookupCenter);
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
