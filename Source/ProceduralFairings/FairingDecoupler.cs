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

        bool decoupled;
        bool didForce;

        public bool updateFightUICheck = true;
        public bool updateEditorUICheck = true;

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
        [UI_Toggle (disabledText = "Off", enabledText = "On")]
        public bool fairingStaged = true;

        [KSPEvent (name = "Jettison", active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false, guiName = "Jettison Fairing")]
        public void OnJettisonFairing ()
        {
            decoupled = fairingStaged;

            OnSetFairingStaging (fairingStaged);
        }

        public void FixedUpdate ()
        {
            //  Set the staging icon visibility (editor only).

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (updateEditorUICheck.Equals (true))
                {
                    //  Get the child fairing parts.

                    var fairingSides = part.symmetryCounterparts;

                    //  Get the number of child fairing parts.

                    int fairingSideNumber = fairingSides.Equals (null) ? 1 : fairingSides.Count;

                    //  Set the staging icon for the parent part.

                    /*if (fairingStaged.Equals (true))
                    {
                        part.stackIcon.CreateIcon ();
                    }
                    else
                    {
                       part.stackIcon.RemoveIcon ();
                    }*/

                    //  Set the staging icon for the child parts.

                    for (int count = 0; count < fairingSideNumber; count++)
                    {
                        if (fairingStaged.Equals (true))
                        {
                            fairingSides [count].stackIcon.CreateIcon ();

                            fairingSides [count].GetComponent<ProceduralFairingDecoupler>().fairingStaged = true;
                            fairingSides [count].GetComponent<ProceduralFairingDecoupler>().OnSetFairingStaging (true);
                        }
                        else
                        {
                            fairingSides [count].stackIcon.RemoveIcon ();

                            fairingSides [count].GetComponent<ProceduralFairingDecoupler>().fairingStaged = false;
                            fairingSides [count].GetComponent<ProceduralFairingDecoupler>().OnSetFairingStaging (false);
                        }
                    }

                    //  Reorder the staging icons.

                    StageManager.Instance.SortIcons (true);

                    //  Tag as done.

                    updateEditorUICheck = false;
                }
            }

            //  Set the staging icon visibility (flight only).

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (updateFightUICheck.Equals (true))
                {
                    if (fairingStaged.Equals (true))
                    {
                        part.stackIcon.CreateIcon ();

                    }
                    else
                    {
                        part.stackIcon.RemoveIcon ();
                    }

                    //  Set the state of the "Jettison" button.

                    OnSetFairingStaging (fairingStaged);

                    //  Reorder the staging icons.

                    StageManager.Instance.SortIcons (true);

                    //  Tag as done.

                    updateFightUICheck = false;
                }
            }

            //  Do the decoupling.

            if (decoupled.Equals (true) && fairingStaged.Equals (true))
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

        public override void OnLoad (ConfigNode node)
        {
            base.OnLoad (node);

            didForce = decoupled;
        }

        void OnSetFairingStaging (bool bFairingStaged)
        {
            Events["OnJettisonFairing"].guiActive = bFairingStaged;
        }

        public override void OnStart (StartState state)
        {
            if (state == StartState.None)
            {
                return;
            }

            ejectFx.audio = part.gameObject.AddComponent<AudioSource>();
            ejectFx.audio.volume = GameSettings.SHIP_VOLUME;
            ejectFx.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            ejectFx.audio.maxDistance = 100;
            ejectFx.audio.loop = false;
            ejectFx.audio.playOnAwake = false;

            if (GameDatabase.Instance.ExistsAudioClip (ejectSoundUrl))
            {
                ejectFx.audio.clip = GameDatabase.Instance.GetAudioClip (ejectSoundUrl);
            }
            else
            {
                Debug.LogError ("[PF]: Cannot find decoupler sound: " + ejectSoundUrl, this);
            }

            //  Set up the GUI update callbacks.

            OnUpdateFairingSideUI ();
        }

        void OnUpdateFairingSideUI ()
        {
            ((UI_Toggle) Fields["fairingStaged"].uiControlEditor).onFieldChanged += OnUpdateUI;
        }

        void OnUpdateUI (BaseField bf, object obj)
        {
            updateEditorUICheck = true;
        }
    }
}
