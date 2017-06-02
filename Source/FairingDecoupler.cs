// Procedural Fairings plug-in by Alexey Volynskov
// Licensed under CC BY 3.0 terms: http://creativecommons.org/licenses/by/3.0/
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;


namespace Keramzit {


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


public class ProceduralFairingDecoupler : PartModule
{
  [KSPField] public float ejectionDv=15;
  [KSPField] public float ejectionTorque=10;
  [KSPField] public float ejectionLowDv=0;
  [KSPField] public float ejectionLowTorque=0;

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Ejection power")]
  [UI_FloatRange(minValue=0, maxValue=1, stepIncrement=0.01f)]
  public float ejectionPower=0.32f;

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Ejection torque")]
  [UI_FloatRange(minValue=0, maxValue=1, stepIncrement=0.01f)]
  public float torqueAmount=0.01f;

  [KSPField] public string ejectSoundUrl="Squad/Sounds/sound_decoupler_fire";
  public FXGroup ejectFx;

  [KSPField] public string transformName="nose_collider";
  [KSPField] public Vector3 forceVector=Vector3.right;
  [KSPField] public Vector3 torqueVector=-Vector3.forward;

  [KSPField(isPersistant=true)] bool decoupled=false;
  bool didForce=false;


  public override void OnStart(StartState state)
  {
    if (state==StartState.None) return;

    ejectFx.audio=part.gameObject.AddComponent<AudioSource>();
    ejectFx.audio.volume=GameSettings.SHIP_VOLUME;
    ejectFx.audio.rolloffMode=AudioRolloffMode.Logarithmic;
    // ejectFx.audio.panLevel=1;
    ejectFx.audio.maxDistance=100;
    ejectFx.audio.loop=false;
    ejectFx.audio.playOnAwake=false;

    if (GameDatabase.Instance.ExistsAudioClip(ejectSoundUrl))
      ejectFx.audio.clip=GameDatabase.Instance.GetAudioClip(ejectSoundUrl);
    else
      Debug.LogError("[ProceduralFairingSide] can't find sound: "+ejectSoundUrl, this);

    //GameEvents.onPartJointBreak.Add(OnPartJointBreak);
  }


  public override void OnLoad(ConfigNode cfg)
  {
    base.OnLoad(cfg);
    didForce=decoupled;
  }

  public void OnDestroy()
  {
      //GameEvents.onPartJointBreak.Remove(OnPartJointBreak);
  }

  //void OnPartJointBreak(PartJoint joint, float data)
  //{
  //    Part p = joint.GetComponent<Part>();
  //    bool isstaging = false;

  //    if (p == this.part)
  //    {
  //        var mod = this.part.GetComponent<ProceduralFairingDecoupler>();
  //        if (mod != null)
  //        {
  //            isstaging = mod.StagingEnabled();
  //            if (isstaging == true)
  //            {
  //                mod.ToggleStaging();
  //                mod.ToggleStaging();
  //            }

  //            isstaging = mod.StagingEnabled();
  //        }
  //    }

  //}

  public void FixedUpdate()
  {
    if (decoupled)
    {
      if (part.parent)
      {
          var pfa = part.parent.GetComponent<ProceduralFairingAdapter>();
          for (int i = 0; i < part.parent.children.Count; i++)
          {
              var p = part.parent.children[i];

              // check if top node allows for decoupling when fairing is gone
              if (pfa)
              {
                  if (!pfa.topNodeDecouplesWhenFairingsGone)
                  {
                      var isFairing = p.GetComponent<ProceduralFairingSide>();
                      if (!isFairing)
                          continue;
                  }
              }

              var joints = p.GetComponents<ConfigurableJoint>();
              for (int j = 0; j < joints.Length; j++)
              {
                  var joint = joints[j];
                  if (joint != null && (joint.GetComponent<Rigidbody>() == part.Rigidbody || joint.connectedBody == part.Rigidbody))
                      Destroy(joint);
              }
          }

          part.decouple(0);
        
        ejectFx.audio.Play();
      }
      else if (!didForce)
      {
        var tr=part.FindModelTransform(transformName);
        if (tr)
        {
          part.Rigidbody.AddForce(tr.TransformDirection(forceVector)
            *Mathf.Lerp(ejectionLowDv, ejectionDv, ejectionPower),
            ForceMode.VelocityChange);
          part.Rigidbody.AddTorque(tr.TransformDirection(torqueVector)
            *Mathf.Lerp(ejectionLowTorque, ejectionTorque, torqueAmount),
            ForceMode.VelocityChange);
        }
        else
          Debug.LogError("[ProceduralFairingDecoupler] no '"+transformName+"' transform in part", part);

        didForce=true;
        decoupled = false;
      }
    }
  }


  [KSPEvent(name = "Jettison", active=true, guiActive=true, guiActiveUnfocused=false, guiName="Jettison")]
  public void Jettison()
  {
    decoupled=true;

    // if (part.parent)
    // {
    //   foreach (var p in part.parent.children)
    //     foreach (var joint in p.GetComponents<ConfigurableJoint>())
    //       if (joint!=null && (joint.rigidbody==part.Rigidbody || joint.connectedBody==part.Rigidbody))
    //         Destroy(joint);

    //   part.decouple(0);

    //   var tr=part.FindModelTransform(transformName);
    //   if (tr)
    //   {
    //     part.Rigidbody.AddForce(tr.TransformDirection(forceVector)
    //       *Mathf.Lerp(ejectionLowDv, ejectionDv, ejectionPower),
    //       ForceMode.VelocityChange);
    //     part.Rigidbody.AddTorque(tr.TransformDirection(torqueVector)
    //       *Mathf.Lerp(ejectionLowTorque, ejectionTorque, torqueAmount),
    //       ForceMode.VelocityChange);
    //   }
    //   else
    //     Debug.LogError("[ProceduralFairingDecoupler] no '"+transformName+"' transform in part", part);

    //   ejectFx.audio.Play();
    // }
  }


  public override void OnActive()
  {
    Jettison();
  }


  [KSPAction("Jettison", actionGroup=KSPActionGroup.None)]
  public void ActionJettison(KSPActionParam param)
  {
    Jettison();
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


} // namespace

