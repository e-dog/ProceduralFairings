// Procedural Fairings plug-in by Alexey Volynskov
// Licensed under CC BY 3.0 terms: http://creativecommons.org/licenses/by/3.0/
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using KSPAPIExtensions;


namespace Keramzit {


public abstract class KzPartResizer : PartModule, IPartCostModifier
{
  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Size", guiFormat="S4", guiUnits="m")]
  [UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.1f, maxValue=5, incrementLarge=1.25f, incrementSmall=0.125f, incrementSlide=0.001f)]
  public float size=1.25f;

  [KSPField] public float diameterStepLarge=1.25f;
  [KSPField] public float diameterStepSmall=0.125f;

  [KSPField] public Vector4 specificMass=new Vector4(0.005f, 0.011f, 0.009f, 0f);
  [KSPField] public float specificBreakingForce =1536;
  [KSPField] public float specificBreakingTorque=1536;
  [KSPField] public float costPerTonne=2000;

  [KSPField] public string minSizeName="PROCFAIRINGS_MINDIAMETER";
  [KSPField] public string maxSizeName="PROCFAIRINGS_MAXDIAMETER";

  [KSPField(isPersistant=false, guiActive=false, guiActiveEditor=true, guiName="Mass")]
  public string massDisplay;

  [KSPField(isPersistant=false, guiActive=false, guiActiveEditor=true, guiName="Cost")]
  public string costDisplay;


  protected float oldSize=-1000;
  protected bool justLoaded=false, limitsSet=false;


  public float GetModuleCost(float defcost)
  {
    return part.mass*costPerTonne;
  }


  public override void OnStart(StartState state)
  {
    base.OnStart(state);
    limitsSet=false;
    updateNodeSize(size);
  }


  public override void OnLoad(ConfigNode cfg)
  {
    base.OnLoad(cfg);
    justLoaded=true;
    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) updateNodeSize(size);
  }


  public virtual void FixedUpdate()
  {
    if (!limitsSet && PFUtils.canCheckTech())
    {
      limitsSet=true;
      float minSize=PFUtils.getTechMinValue(minSizeName, 0.25f);
      float maxSize=PFUtils.getTechMaxValue(maxSizeName, 30);

      PFUtils.setFieldRange(Fields["size"], minSize, maxSize);

      ((UI_FloatEdit)Fields["size"].uiControlEditor).incrementLarge=diameterStepLarge;
      ((UI_FloatEdit)Fields["size"].uiControlEditor).incrementSmall=diameterStepSmall;
    }

    if (size!=oldSize)
    {
      resizePart(size);
      PFUtils.updateDragCube(part);
    }

    justLoaded=false;
  }


  public void scaleNode(AttachNode node, float scale, bool setSize)
  {
    if (node==null) return;
    node.position=node.originalPosition*scale;
    if (!justLoaded) PFUtils.updateAttachedPartPos(node, part);
    if (setSize) node.size=Mathf.RoundToInt(scale/diameterStepLarge);
  }


  public void setNodeSize(AttachNode node, float scale)
  {
    if (node==null) return;
    node.size=Mathf.RoundToInt(scale/diameterStepLarge);
  }


  public virtual void updateNodeSize(float scale)
  {
    setNodeSize(part.findAttachNode("top"   ), scale);
    setNodeSize(part.findAttachNode("bottom"), scale);
  }


  public virtual void resizePart(float scale)
  {
    oldSize=size;

    part.mass=((specificMass.x*scale+specificMass.y)*scale+specificMass.z)*scale+specificMass.w;
    massDisplay=PFUtils.formatMass(part.mass);
    costDisplay=PFUtils.formatCost(GetModuleCost(0)+part.partInfo.cost);
    part.breakingForce =specificBreakingForce *Mathf.Pow(scale, 2);
    part.breakingTorque=specificBreakingTorque*Mathf.Pow(scale, 2);

    var model=part.FindModelTransform("model");
    if (model!=null) model.localScale=Vector3.one*scale;
    else Debug.LogError("[KzPartResizer] No 'model' transform in the part", this);
    part.rescaleFactor=scale;

    scaleNode(part.findAttachNode("top"   ), scale, true);
    scaleNode(part.findAttachNode("bottom"), scale, true);
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


public class KzFairingBaseResizer : KzPartResizer
{
  [KSPField] public float sideThickness=0.05f/1.25f;


  public float calcSideThickness()
  {
    return Mathf.Min(sideThickness*size, size*0.25f);
  }


  public override void updateNodeSize(float scale)
  {
    float sth=calcSideThickness();
    float br=size*0.5f-sth;
    scale=br*2;

    base.updateNodeSize(scale);

    int sideNodeSize=Mathf.RoundToInt(scale/diameterStepLarge)-1;
    if (sideNodeSize<0) sideNodeSize=0;

    foreach (var n in part.findAttachNodes("connect"))
      n.size=sideNodeSize;
  }


  public override void resizePart(float scale)
  {
    float sth=calcSideThickness();
    float br=size*0.5f-sth;
    scale=br*2;

    base.resizePart(scale);

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
      nnt.radius=size*0.5f;
    }

    var fbase=part.GetComponent<ProceduralFairingBase>();
    if (fbase)
    {
      fbase.baseSize=br*2;
      fbase.sideThickness=sth;
      fbase.needShapeUpdate=true;
    }
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


public class KzThrustPlateResizer : KzPartResizer
{
  public override void resizePart(float scale)
  {
    base.resizePart(scale);

    var node=part.findAttachNode("bottom");

    foreach (var n in part.findAttachNodes("bottom"))
    {
      n.position.y=node.position.y;
      if (!justLoaded) PFUtils.updateAttachedPartPos(n, part);
    }

    var nnt=part.GetComponent<KzNodeNumberTweaker>();
    if (nnt)
    {
      float mr=size*0.5f;
      if (nnt.radius>mr) nnt.radius=mr;
      ((UI_FloatEdit)nnt.Fields["radius"].uiControlEditor).maxValue=mr;
    }
  }
}


//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


} // namespace
