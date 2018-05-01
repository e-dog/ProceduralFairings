//  ==================================================
//  Procedural Fairings plug-in by Alexey Volynskov.

//  Licensed under CC-BY-4.0 terms: https://creativecommons.org/licenses/by/4.0/legalcode
//  ==================================================

using KSP.UI.Screens;
using System;
using UnityEngine;

namespace Keramzit
{
    public class ProceduralFairingDecoupler : PartModule
    {
        [KSPField] public float ejectionDv = 15;
        [KSPField] public float ejectionTorque = 10;
        [KSPField] public float ejectionLowDv;
        [KSPField] public float ejectionLowTorque;

        [KSPField (isPersistant = true)] bool decoupled;

        bool didForce;

        bool decouplerStagingSet = true;

        [KSPField] public string ejectSoundUrl = "Squad/Sounds/sound_decoupler_fire";
        public FXGroup ejectFx;

        [KSPField] public string transformName = "nose_collider";
        [KSPField] public Vector3 forceVector = Vector3.right;
        [KSPField] public Vector3 torqueVector = -Vector3.forward;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Ejection power")]
        [UI_FloatRange (minValue = 0, maxValue = 1, stepIncrement = 0.01f)]
        public float ejectionPower = 0.32f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Ejection torque")]
        [UI_FloatRange (minValue = 0, maxValue = 1, stepIncrement = 0.01f)]
        public float torqueAmount = 0.01f;

        [KSPField (isPersistant = true, guiActiveEditor = true, guiName = "Fairing Decoupler")]
        [UI_Toggle (disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool fairingStaged = true;

        [KSPAction ("Jettison Fairing", actionGroup = KSPActionGroup.None)]
        public void ActionJettison (KSPActionParam param)
        {
            OnJettisonFairing ();
        }

        public void FixedUpdate ()
        {
            //  More hacky-hacky: for some reason the staging icons cannot be updated correctly
            //  via the OnStart () method but require an additional update here. This snippet
            //  sets the staging icon states one more time after transitioning a scene.

            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                if (decouplerStagingSet)
                {
                    OnSetStagingIcons ();

                    decouplerStagingSet = false;
                }
            }

            if (decoupled)
            {
                if (part.parent)
                {
                    var pfa = part.parent.GetComponent<ProceduralFairingAdapter>();

                    for (int i = 0; i < part.parent.children.Count; i++)
                    {
                        var p = part.parent.children [i];

                        //  Check if the top node allows decoupling when the fairing is also decoupled.

                        if (pfa)
                        {
                            if (!pfa.topNodeDecouplesWhenFairingsGone)
                            {
                                var isFairing = p.GetComponent<ProceduralFairingSide>();

                                if (!isFairing)
                                {
                                    continue;
                                }
                            }
                        }

                        var joints = p.GetComponents<ConfigurableJoint>();

                        for (int j = 0; j < joints.Length; j++)
                        {
                            var joint = joints [j];

                            if (joint != null && (joint.GetComponent<Rigidbody>() == part.Rigidbody || joint.connectedBody == part.Rigidbody))
                            {
                                Destroy (joint);
                            }
                        }
                    }

                    part.decouple (0);

                    ejectFx.audio.Play ();
                }
                else if (!didForce)
                {
                    var tr = part.FindModelTransform (transformName);

                    if (tr)
                    {
                        part.Rigidbody.AddForce (tr.TransformDirection (forceVector) * Mathf.Lerp (ejectionLowDv, ejectionDv, ejectionPower), ForceMode.VelocityChange);
                        part.Rigidbody.AddTorque (tr.TransformDirection (torqueVector) * Mathf.Lerp (ejectionLowTorque, ejectionTorque, torqueAmount), ForceMode.VelocityChange);
                    }
                    else
                    {
                        Debug.LogError ("[PF]: No '" + transformName + "' transform in part!", part);
                    }

                    didForce = true;
                    decoupled = false;
                }
            }
        }

        public override void OnActive ()
        {
            OnJettisonFairing ();
        }

        [KSPEvent (name = "Jettison", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "Jettison Fairing")]
        public void OnJettisonFairing ()
        {
            decoupled |= fairingStaged;
        }

        public override void OnLoad (ConfigNode node)
        {
            base.OnLoad (node);

            didForce = decoupled;
        }

        public void OnSetStagingIcons ()
        {
            //  Set the staging icon for the parent part.

            if (fairingStaged)
            {
                part.stackIcon.CreateIcon ();
            }
            else
            {
                part.stackIcon.RemoveIcon ();
            }

            //  Hacky, hacky? Five dollars...

            foreach (Part FairingSide in part.symmetryCounterparts)
            {
                if (fairingStaged)
                {
                    FairingSide.stackIcon.CreateIcon ();
                }
                else
                {
                    FairingSide.stackIcon.RemoveIcon ();
                }
            }

            //  Reorder the staging icons.

            StageManager.Instance.SortIcons (true);
        }

        public override void OnStart (StartState state)
        {
            if (state == StartState.None)
            {
                return;
            }

            if (state == StartState.Editor)
            {
                //  Set up the GUI editor update callback.

                ((UI_Toggle) Fields["fairingStaged"].uiControlEditor).onFieldChanged += OnUpdateUI;
            }

            ejectFx.audio = part.gameObject.AddComponent<AudioSource>();
            ejectFx.audio.volume = GameSettings.SHIP_VOLUME;
            ejectFx.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            ejectFx.audio.maxDistance = 100;
            ejectFx.audio.loop = false;
            ejectFx.audio.playOnAwake = false;
            ejectFx.audio.dopplerLevel = 0f;
            ejectFx.audio.spatialBlend = 1.0f;
            ejectFx.audio.panStereo = 0f;

            if (GameDatabase.Instance.ExistsAudioClip (ejectSoundUrl))
            {
                ejectFx.audio.clip = GameDatabase.Instance.GetAudioClip (ejectSoundUrl);
            }
            else
            {
                Debug.LogError ("[PF]: Cannot find decoupler sound: " + ejectSoundUrl, this);
            }

            //  Set the state of the "Jettison Fairing" PAW button.

            Events["OnJettisonFairing"].guiActive = fairingStaged;

            //  Update the staging icon sequence.

            OnSetStagingIcons ();
        }

        void OnUpdateUI (BaseField bf, object obj)
        {
            //  Update the staging icon sequence.

            OnSetStagingIcons ();
        }
   }
}
