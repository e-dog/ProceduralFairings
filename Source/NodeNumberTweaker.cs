// Procedural Fairings plug-in by Alexey Volynskov
// Licensed under CC BY 3.0 terms: http://creativecommons.org/licenses/by/3.0/
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;


namespace Keramzit {


public class KzNodeNumberTweaker : PartModule
{
  [KSPField] public string nodePrefix="bottom";
  [KSPField] public int maxNumber=0;

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Nodes")]
  public int numNodes=0;

  [KSPField(isPersistant=true, guiActiveEditor=true, guiName="Node offset", guiFormat="S4", guiUnits="m")]
  [UI_FloatEdit(sigFigs=3, unit="m", minValue=0.1f, maxValue=5, incrementLarge=0.625f, incrementSmall=0.125f, incrementSlide=0.001f)]
  public float radius=1.25f;

  [KSPField] public float radiusStepLarge=0.625f;
  [KSPField] public float radiusStepSmall=0.125f;


  [KSPField] public bool shouldResizeNodes=true;


  protected float oldRadius=-1000;
  protected bool justLoaded=false;


  public override string GetInfo()
  {
    return "Max. nodes: "+maxNumber;
  }


  [KSPEvent(name="IncrementNodes", active=true, guiActive=false, guiActiveEditor=true,
    guiActiveUnfocused=false, guiName="More nodes")]
  public void IncrementNodes()
  {
    if (numNodes>=maxNumber) return;
    if (checkNodeAttachments()) return;

    ++numNodes;
    updateNodes();
  }


  [KSPEvent(name="DecrementNodes", active=true, guiActive=false, guiActiveEditor=true,
    guiActiveUnfocused=false, guiName="Fewer nodes")]
  public void DecrementNodes()
  {
    if (numNodes<=1) return;
    if (checkNodeAttachments()) return;

    --numNodes;
    updateNodes();
  }


  public virtual void FixedUpdate()
  {
    if (radius!=oldRadius) { oldRadius=radius; updateNodePositions(); }
    justLoaded=false;
  }


  public void Update()
  {
    if (HighLogic.LoadedSceneIsEditor)
    {
      bool removed=false;

      for (int i=numNodes+1; i<=maxNumber; ++i)
      {
        var node=findNode(i);
        if (node==null) continue;
        part.attachNodes.Remove(node);
        removed=true;
      }

      if (removed)
      {
        var fbase=part.GetComponent<ProceduralFairingBase>();
        if (fbase) { fbase.needShapeUpdate=true; fbase.updateDelay=0; }
      }
    }
  }


  public override void OnStart(StartState state)
  {
    // print("NNT: OnStart "+state);
    base.OnStart(state);
    if (state==StartState.None) return;

    ((UI_FloatEdit)Fields["radius"].uiControlEditor).incrementLarge=radiusStepLarge;
    ((UI_FloatEdit)Fields["radius"].uiControlEditor).incrementSmall=radiusStepSmall;

    if (!shouldResizeNodes)
    {
      Fields["radius"].guiActiveEditor=false;
    }

    updateNodes();
  }


  public override void OnLoad(ConfigNode cfg)
  {
    // print("NNT: OnLoad");
    base.OnLoad(cfg);
    justLoaded=true;
    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) updateNodes();
  }


  void updateNodes()
  {
    addRemoveNodes();
    updateNodePositions();
  }


  string nodeName(int i) { return string.Format("{0}{1:d2}", nodePrefix, i); }
  AttachNode findNode(int i) { return part.findAttachNode(nodeName(i)); }


  bool checkNodeAttachments()
  {
    for (int i=1; i<=numNodes; ++i)
    {
      var node=findNode(i);
      if (node!=null && node.attachedPart!=null)
      {
        EditorScreenMessager.showMessage("Please detach parts before changing number of nodes", 1);
        return true;
      }
    }

    return false;
  }


  void addRemoveNodes()
  {
    // print("NNT: setting nodes to "+numNodes);
    part.stackSymmetry=numNodes-1;

    float y=0;
    bool gotY=false;
    int nodeSize=0;
    Vector3 dir=Vector3.up;

    int i;
    for (i=1; i<=maxNumber; ++i)
    {
      var node=findNode(i);
      if (node==null) continue;
      y=node.position.y;
      nodeSize=node.size;
      dir=node.orientation;
      gotY=true;
      break;
    }

    if (!gotY)
    {
      var node=part.findAttachNode("bottom");
      if (node!=null) y=node.position.y;
    }

    for (i=1; i<=numNodes; ++i)
    {
      var node=findNode(i);
      if (node!=null) continue;

      // create node
      node=new AttachNode();
      node.id=nodeName(i);
      node.owner=part;
      node.nodeType=AttachNode.NodeType.Stack;
      node.position=new Vector3(0, y, 0);
      node.orientation=dir;
      node.originalPosition=node.position;
      node.originalOrientation=node.orientation;
      node.size=nodeSize;
      part.attachNodes.Add(node);
    }

    for (; i<=maxNumber; ++i)
    {
      var node=findNode(i);
      if (node==null) continue;

      if (HighLogic.LoadedSceneIsEditor) node.position=new Vector3(10000, 0, 0);
      else part.attachNodes.Remove(node);
    }

    var fbase=part.GetComponent<ProceduralFairingBase>();
    if (fbase) fbase.needShapeUpdate=true;
  }


  void updateNodePositions()
  {
    float d=Mathf.Sin(Mathf.PI/numNodes)*radius*2;
    int size=Mathf.RoundToInt(d/(radiusStepLarge*2));

    for (int i=1; i<=numNodes; ++i)
    {
      var node=findNode(i);
      if (node==null) continue;

      float a=Mathf.PI*2*(i-1)/numNodes;
      node.position.x=Mathf.Cos(a)*radius;
      node.position.z=Mathf.Sin(a)*radius;
      if (shouldResizeNodes) node.size=size;

      if (!justLoaded) PFUtils.updateAttachedPartPos(node, part);
    }

    for (int i=numNodes+1; i<=maxNumber; ++i)
    {
      var node=findNode(i);
      if (node==null) continue;

      node.position=new Vector3(10000, 0, 0);
    }
  }
}


} // namespace
