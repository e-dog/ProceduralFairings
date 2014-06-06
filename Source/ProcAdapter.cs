// Procedural Fairings plug-in by Alexey Volynskov
// Licensed under CC BY 3.0 terms: http://creativecommons.org/licenses/by/3.0/
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using KSPAPIExtensions;


namespace Keramzit {


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


abstract class ProceduralAdapterBase : PartModule
{
  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Base", guiFormat="S4", guiUnits="m")]
  [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.1f, maxValue=5, incrementLarge=1.25f, incrementSmall=0.125f, incrementSlide=0.001f)]
  public float baseSize=1.25f;

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Top", guiFormat="S4", guiUnits="m")]
  [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.1f, maxValue=5, incrementLarge=1.25f, incrementSmall=0.125f, incrementSlide=0.001f)]
  public float topSize=1.25f;

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Height", guiFormat="S4", guiUnits="m")]
  [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.1f, maxValue=50, incrementLarge=1.0f, incrementSmall=0.1f, incrementSlide=0.001f)]
  public float height=1;

  [KSPField] public string  topNodeName="top1";

  [KSPField] public float diameterStepLarge=1.25f;
  [KSPField] public float diameterStepSmall=0.125f;

  [KSPField] public float heightStepLarge=1.0f;
  [KSPField] public float heightStepSmall=0.1f;

  public bool changed=true;

  abstract public float minHeight { get; }


  private float lastBaseSize=-1000;
  private float lastTopSize=-1000;
  private float lastHeight=-1000;

  protected bool justLoaded=false;


  public virtual void checkTweakables()
  {
    if (baseSize!=lastBaseSize) { lastBaseSize=baseSize; changed=true; }
    if (topSize!=lastTopSize) { lastTopSize=topSize; changed=true; }
    if (height!=lastHeight) { lastHeight=height; changed=true; }
  }


  public virtual void FixedUpdate()
  {
    checkTweakables();
    if (changed) updateShape();
    justLoaded=false;
  }


  public virtual void updateShape()
  {
    changed=false;

    var node=part.findAttachNode("bottom");
    if (node!=null) node.size=Mathf.RoundToInt(baseSize/diameterStepLarge);

    node=part.findAttachNode("top");
    if (node!=null) node.size=Mathf.RoundToInt(baseSize/diameterStepLarge);

    node=part.findAttachNode(topNodeName);
    if (node!=null)
    {
      node.position=new Vector3(0, height, 0);
      node.size=Mathf.RoundToInt(topSize/diameterStepLarge);
      if (!justLoaded) PFUtils.updateAttachedPartPos(node, part);
    }
    else
      Debug.LogError("[ProceduralAdapterBase] No '"+topNodeName+"' node in part", this);
  }


  public override void OnStart(StartState state)
  {
    base.OnStart(state);

    if (state==StartState.None) return;

    changed=true;
  }


  public override void OnLoad(ConfigNode cfg)
  {
    base.OnLoad(cfg);
    justLoaded=true;
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


class ProceduralFairingAdapter : ProceduralAdapterBase
{
  [KSPField] public float sideThickness=0.05f/1.25f;
  [KSPField] public Vector4 specificMass=new Vector4(0.005f, 0.011f, 0.009f, 0f);
  [KSPField] public float specificBreakingForce =6050;
  [KSPField] public float specificBreakingTorque=6050;

  public override float minHeight { get { return baseSize*0.2f; } }

  public float calcSideThickness()
  {
    return Mathf.Min(
      sideThickness*Mathf.Max(baseSize, topSize),
      Mathf.Min(baseSize, topSize)*0.25f);
  }

  public float topRadius { get { return topSize*0.5f-calcSideThickness(); } }

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Extra height", guiFormat="S4", guiUnits="m")]
  [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0, maxValue=50, incrementLarge=1.0f, incrementSmall=0.1f, incrementSlide=0.001f)]
  public float extraHeight=0;

  public bool engineFairingRemoved=false;

  [KSPField(isPersistant=false, guiActive=false, guiActiveEditor=true, guiName="Mass")]
  public string massDisplay;


  private float lastExtraHt=-1000;

  public override void checkTweakables()
  {
    base.checkTweakables();
    if (extraHeight!=lastExtraHt) { lastExtraHt=extraHeight; changed=true; }
  }


  public override void OnStart(StartState state)
  {
    base.OnStart(state);

    if (HighLogic.LoadedSceneIsEditor)
    {
      float minSize=PFUtils.getTechMinValue("PROCFAIRINGS_MINDIAMETER", 0.25f);
      float maxSize=PFUtils.getTechMaxValue("PROCFAIRINGS_MAXDIAMETER", 30);

      PFUtils.setFieldRange(Fields["baseSize"], minSize, maxSize);
      PFUtils.setFieldRange(Fields[ "topSize"], minSize, maxSize);

      ((UI_FloatEdit)Fields["baseSize"].uiControlEditor).incrementLarge=diameterStepLarge;
      ((UI_FloatEdit)Fields["baseSize"].uiControlEditor).incrementSmall=diameterStepSmall;
      ((UI_FloatEdit)Fields[ "topSize"].uiControlEditor).incrementLarge=diameterStepLarge;
      ((UI_FloatEdit)Fields[ "topSize"].uiControlEditor).incrementSmall=diameterStepSmall;

      ((UI_FloatEdit)Fields["height"].uiControlEditor).incrementLarge=heightStepLarge;
      ((UI_FloatEdit)Fields["height"].uiControlEditor).incrementSmall=heightStepSmall;
      ((UI_FloatEdit)Fields["extraHeight"].uiControlEditor).incrementLarge=heightStepLarge;
      ((UI_FloatEdit)Fields["extraHeight"].uiControlEditor).incrementSmall=heightStepSmall;
    }
  }


  public override void updateShape()
  {
    base.updateShape();

    float sth=calcSideThickness();
    float br=baseSize*0.5f-sth;
    float scale=br*2;

    part.mass=((specificMass.x*scale+specificMass.y)*scale+specificMass.z)*scale+specificMass.w;
    massDisplay=PFUtils.formatMass(part.mass);
    part.breakingForce =specificBreakingForce *Mathf.Pow(br, 2);
    part.breakingTorque=specificBreakingTorque*Mathf.Pow(br, 2);

    var model=part.FindModelTransform("model");
    if (model!=null) model.localScale=Vector3.one*scale;
    else Debug.LogError("[ProceduralFairingAdapter] No 'model' transform in the part", this);

    var node=part.findAttachNode("top");
    node.position=node.originalPosition*scale;
    if (!justLoaded) PFUtils.updateAttachedPartPos(node, part);

    var    topNode=part.findAttachNode("top"   );
    var bottomNode=part.findAttachNode("bottom");

    float y=(topNode.position.y+bottomNode.position.y)*0.5f;
    int sideNodeSize=Mathf.RoundToInt(scale/diameterStepLarge)-1;
    if (sideNodeSize<0) sideNodeSize=0;

    foreach (var n in part.findAttachNodes("connect"))
    {
      n.position.y=y;
      n.size=sideNodeSize;
      if (!justLoaded) PFUtils.updateAttachedPartPos(n, part);
    }

    var nnt=part.GetComponent<KzNodeNumberTweaker>();
    if (nnt)
    {
      nnt.radius=baseSize*0.5f;
    }

    var fbase=part.GetComponent<ProceduralFairingBase>();
    if (fbase)
    {
      fbase.baseSize=br*2;
      fbase.sideThickness=sth;
      fbase.updateDelay=0;
    }
  }


  public override void FixedUpdate()
  {
    base.FixedUpdate();

    if (!engineFairingRemoved)
    {
      var node=part.findAttachNode(topNodeName);
      if (node!=null && node.attachedPart!=null)
      {
        var tp=node.attachedPart;

        if (HighLogic.LoadedSceneIsEditor || !tp.packed)
        {
          foreach (var mj in tp.GetComponents<ModuleJettison>())
          {
            // print("[ProceduralFairingAdapter] removing engine fairings "+mj);
            var jt=tp.FindModelTransform(mj.jettisonName);
            if (jt==null) jt=mj.jettisonTransform;
            if (jt!=null)
            {
              // print("[ProceduralFairingAdapter] disabling engine fairing "+jt);
              jt.gameObject.SetActive(false);
            }

            mj.jettisonName=null;
            mj.jettisonTransform=null;

            // tp.RemoveModule(mj);
          }

          if (!HighLogic.LoadedSceneIsEditor) engineFairingRemoved=true;
        }
      }
    }

    if (!HighLogic.LoadedSceneIsEditor)
    {
      var node=part.findAttachNode(topNodeName);
      if (node!=null && node.attachedPart!=null)
      {
        var tp=node.attachedPart;

        foreach (var n in part.findAttachNodes("connect"))
          if (n.attachedPart!=null) return;

        if (part.parent==tp) part.decouple(0);
        else if (tp.parent==part) tp.decouple(0);
        else
          Debug.LogError("[ProceduralFairingAdapter] Can't decouple from top part", this);
      }
    }
  }


  public Part getTopPart()
  {
    var node=part.findAttachNode(topNodeName);
    if (node==null) return null;
    return node.attachedPart;
  }


  public override void OnLoad(ConfigNode cfg)
  {
    base.OnLoad(cfg);

    if (cfg.HasValue("baseRadius") && cfg.HasValue("topRadius"))
    {
      // load legacy settings
      float br=float.Parse(cfg.GetValue("baseRadius"));
      float tr=float.Parse(cfg.GetValue( "topRadius"));
      baseSize=(br+sideThickness*br)*2;
      topSize=(tr+sideThickness*br)*2;
      sideThickness*=1.15f/1.25f;
    }
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


} // namespace
