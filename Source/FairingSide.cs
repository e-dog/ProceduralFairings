// Procedural Fairings plug-in by Alexey Volynskov
// Licensed under CC BY 3.0 terms: http://creativecommons.org/licenses/by/3.0/
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Keramzit
{


	//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


	public class ProceduralFairingSide : PartModule, IPartCostModifier, IPartMassModifier
	{
		[KSPField] public float noseHeightRatio = 2;
		[KSPField] public float minBaseConeAngle = 20;
		[KSPField] public Vector4 baseConeShape = new Vector4(0.5f, 0, 1, 0.5f);
		[KSPField] public Vector4 noseConeShape = new Vector4(0.5f, 0, 1, 0.5f);
		[KSPField] public int baseConeSegments = 5;
		[KSPField] public int noseConeSegments = 7;

		[KSPField] public Vector2 mappingScale = new Vector2(1024, 1024);
		[KSPField] public Vector2 stripMapping = new Vector2(992, 1024);
		[KSPField] public Vector4 horMapping = new Vector4(0, 480, 512, 992);
		[KSPField] public Vector4 vertMapping = new Vector4(0, 160, 704, 1024);

		[KSPField] public float density = 0.2f;
		[KSPField] public float costPerTonne = 2000;
		[KSPField] public float specificBreakingForce = 2000;
		[KSPField] public float specificBreakingTorque = 2000;

		[KSPField(isPersistant = true)] public int numSegs = 12;
		[KSPField(isPersistant = true)] public int numSideParts = 2;
		[KSPField(isPersistant = true)] public float baseRad = 0;
		[KSPField(isPersistant = true)] public float maxRad = 1.50f;
		[KSPField(isPersistant = true)] public float cylStart = 0.5f;
		[KSPField(isPersistant = true)] public float cylEnd = 2.5f;
		[KSPField(isPersistant = true)] public float topRad = 0;
		[KSPField(isPersistant = true)] public float inlineHeight = 0;
		[KSPField(isPersistant = true)] public float sideThickness = 0.05f;
		[KSPField(isPersistant = true)] public Vector3 meshPos = Vector3.zero;
		[KSPField(isPersistant = true)] public Quaternion meshRot = Quaternion.identity;

		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Shape")]
		[UI_Toggle(disabledText = "Unlocked", enabledText = "Locked")]
		public bool shapeLock = false;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Mass")]
		public string massDisplay;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Cost")]
		public string costDisplay;


		public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.FIXED; }
		public ModifierChangeWhen GetModuleMassChangeWhen() { return ModifierChangeWhen.FIXED; }

		public float GetModuleCost(float defcost, ModifierStagingSituation sit)
		{
			return totalMass * costPerTonne - defcost;
		}

		public float GetModuleMass(float defmass, ModifierStagingSituation sit)
		{
			return totalMass - defmass;
		}


		public override string GetInfo()
		{
			string s = "Attach to Keramzit's fairing base to reshape.";

			return s;
		}


		public void Start()
		{
			part.mass = totalMass;
		}
		public override void OnStart(StartState state)
		{
			if (state == StartState.None) return;

			if (state != StartState.Editor || shapeLock) rebuildMesh();
			part.mass = totalMass;
		}


		public override void OnLoad(ConfigNode cfg)
		{
			base.OnLoad(cfg);
			if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) rebuildMesh();
		}


		public void updateNodeSize()
		{
			var node = part.FindAttachNode("connect");
			if (node != null) {
				int s = Mathf.RoundToInt(baseRad * 2 / 1.25f) - 1;
				if (s < 0) s = 0;
				node.size = s;
			}
		}


		public void FixedUpdate()
		{
			if (HighLogic.LoadedSceneIsEditor) {
				int nsym = part.symmetryCounterparts.Count;
				if (nsym == 0) {
					massDisplay = PFUtils.formatMass(totalMass);
					costDisplay = PFUtils.formatCost(part.partInfo.cost + GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT));
				}
				else if (nsym == 1) {
					massDisplay = PFUtils.formatMass(totalMass * 2) + " (both)";
					costDisplay = PFUtils.formatCost((part.partInfo.cost + GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT)) * 2) + " (both)";
				}
				else {
					massDisplay = PFUtils.formatMass(totalMass * (nsym + 1)) + " (all " + (nsym + 1) + ")";
					costDisplay = PFUtils.formatCost((part.partInfo.cost + GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT)) * (nsym + 1)) + " (all " + (nsym + 1) + ")";
				}
			}
		}

		public float totalMass;
		public void rebuildMesh()
		{
			var mf = part.FindModelComponent<MeshFilter>("model");
			if (!mf) { Debug.LogError("[ProceduralFairingSide] no model in side fairing", part); return; }

			Mesh m = mf.mesh;
			if (!m) { Debug.LogError("[ProceduralFairingSide] no mesh in side fairing", part); return; }

			mf.transform.localPosition = meshPos;
			mf.transform.localRotation = meshRot;

			updateNodeSize();

			// build fairing shape line
			float tip = maxRad * noseHeightRatio;

			Vector3[] shape;
			if (inlineHeight <= 0)
				shape = ProceduralFairingBase.buildFairingShape(
				  baseRad, maxRad, cylStart, cylEnd, noseHeightRatio,
				  baseConeShape, noseConeShape, baseConeSegments, noseConeSegments,
				  vertMapping, mappingScale.y);
			else
				shape = ProceduralFairingBase.buildInlineFairingShape(
				  baseRad, maxRad, topRad, cylStart, cylEnd, inlineHeight,
				  baseConeShape, baseConeSegments,
				  vertMapping, mappingScale.y);

			// set up params
			var dirs = new Vector3[numSegs + 1];
			for (int i = 0; i <= numSegs; ++i) {
				float a = Mathf.PI * 2 * (i - numSegs * 0.5f) / (numSideParts * numSegs);
				dirs[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
			}

			float segOMappingScale = (horMapping.y - horMapping.x) / (mappingScale.x * numSegs);
			float segIMappingScale = (horMapping.w - horMapping.z) / (mappingScale.x * numSegs);
			float segOMappingOfs = horMapping.x / mappingScale.x;
			float segIMappingOfs = horMapping.z / mappingScale.x;

			if (numSideParts > 2) {
				segOMappingOfs += segOMappingScale * numSegs * (0.5f - 1f / numSideParts);
				segOMappingScale *= 2f / numSideParts;
				segIMappingOfs += segIMappingScale * numSegs * (0.5f - 1f / numSideParts);
				segIMappingScale *= 2f / numSideParts;
			}

			float stripU0 = stripMapping.x / mappingScale.x;
			float stripU1 = stripMapping.y / mappingScale.x;

			float ringSegLen = baseRad * Mathf.PI * 2 / (numSegs * numSideParts);
			float topRingSegLen = topRad * Mathf.PI * 2 / (numSegs * numSideParts);

			float collWidth = maxRad * Mathf.PI * 2 / (numSideParts * 3);

			int numMainVerts = (numSegs + 1) * (shape.Length - 1) + 1;
			int numMainFaces = numSegs * ((shape.Length - 2) * 2 + 1);

			int numSideVerts = shape.Length * 2;
			int numSideFaces = (shape.Length - 1) * 2;

			int numRingVerts = (numSegs + 1) * 2;
			int numRingFaces = numSegs * 2;

			if (inlineHeight > 0) {
				numMainVerts = (numSegs + 1) * shape.Length;
				numMainFaces = numSegs * (shape.Length - 1) * 2;
			}

			int totalVerts = numMainVerts * 2 + numSideVerts * 2 + numRingVerts;
			int totalFaces = numMainFaces * 2 + numSideFaces * 2 + numRingFaces;

			if (inlineHeight > 0) {
				totalVerts += numRingVerts;
				totalFaces += numRingFaces;
			}

			var p = shape[shape.Length - 1];
			float topY = p.y, topV = p.z;

			float collCenter = (cylStart + cylEnd) / 2, collHeight = cylEnd - cylStart;
			if (collHeight <= 0) collHeight = Mathf.Min(topY - cylEnd, cylStart) / 2;

			// compute area
			double area = 0;
			for (int i = 1; i < shape.Length; ++i)
				area += (shape[i - 1].x + shape[i].x) * (shape[i].y - shape[i - 1].y) * Mathf.PI / numSideParts;

			// set params based on volume
			float volume = (float)(area * sideThickness);
			part.mass = totalMass = volume * density;
			part.breakingForce = part.mass * specificBreakingForce;
			part.breakingTorque = part.mass * specificBreakingTorque;

			var offset = new Vector3(maxRad * 0.7f, topY * 0.5f, 0);
			part.CoMOffset = part.transform.InverseTransformPoint(mf.transform.TransformPoint(offset));

			// remove old colliders
			var colls = part.FindModelComponents<Collider>();
			for (int i = 0; i < colls.Count; i++) {
				var c = colls[i];
				// if (c.transform.parent!=mf.transform || c.transform.parent!=mf.transform.parent) continue;
				UnityEngine.Object.Destroy(c.gameObject);
			}

			// add new colliders
			for (int i = -1; i <= 1; ++i) {
				var obj = new GameObject("collider");
				obj.transform.parent = mf.transform;
				obj.transform.localPosition = Vector3.zero;
				obj.transform.localRotation = Quaternion.AngleAxis(90f * i / numSideParts, Vector3.up);
				var coll = obj.AddComponent<BoxCollider>();
				coll.center = new Vector3(maxRad + sideThickness * 0.5f, collCenter, 0);
				coll.size = new Vector3(sideThickness, collHeight, collWidth);
			}

			{
				// nose collider
				float r = maxRad * 0.2f;
				var obj = new GameObject("nose_collider");
				obj.transform.parent = mf.transform;
				obj.transform.localPosition = new Vector3(r, cylEnd + tip - r * 1.2f, 0);
				obj.transform.localRotation = Quaternion.identity;

				if (inlineHeight > 0) {
					r = sideThickness * 0.5f;
					obj.transform.localPosition = new Vector3(maxRad + r, collCenter, 0);
				}

				var coll = obj.AddComponent<SphereCollider>();
				coll.center = Vector3.zero;
				coll.radius = r;
			}

			// build mesh
			m.Clear();

			var verts = new Vector3[totalVerts];
			var uv = new Vector2[totalVerts];
			var norm = new Vector3[totalVerts];
			var tang = new Vector4[totalVerts];

			if (inlineHeight <= 0) {
				// tip vertex
				verts[numMainVerts - 1].Set(0, topY + sideThickness, 0); // outside
				verts[numMainVerts * 2 - 1].Set(0, topY, 0); // inside
				uv[numMainVerts - 1].Set(segOMappingScale * 0.5f * numSegs + segOMappingOfs, topV);
				uv[numMainVerts * 2 - 1].Set(segIMappingScale * 0.5f * numSegs + segIMappingOfs, topV);
				norm[numMainVerts - 1] = Vector3.up;
				norm[numMainVerts * 2 - 1] = -Vector3.up;
				// tang[numMainVerts  -1]= Vector3.forward;
				// tang[numMainVerts*2-1]=-Vector3.forward;
				tang[numMainVerts - 1] = Vector3.zero;
				tang[numMainVerts * 2 - 1] = Vector3.zero;
			}

			// main vertices
			float noseV0 = vertMapping.z / mappingScale.y;
			float noseV1 = vertMapping.w / mappingScale.y;
			float noseVScale = 1f / (noseV1 - noseV0);
			float oCenter = (horMapping.x + horMapping.y) / (mappingScale.x * 2);
			float iCenter = (horMapping.z + horMapping.w) / (mappingScale.x * 2);

			int vi = 0;
			for (int i = 0; i < shape.Length - (inlineHeight <= 0 ? 1 : 0); ++i) {
				p = shape[i];

				Vector2 n;
				if (i == 0) n = shape[1] - shape[0];
				else if (i == shape.Length - 1) n = shape[i] - shape[i - 1];
				else n = shape[i + 1] - shape[i - 1];
				n.Set(n.y, -n.x);
				n.Normalize();

				for (int j = 0; j <= numSegs; ++j, ++vi) {
					var d = dirs[j];
					var dp = d * p.x + Vector3.up * p.y;
					var dn = d * n.x + Vector3.up * n.y;
					if (i == 0 || i == shape.Length - 1) verts[vi] = dp + d * sideThickness;
					else verts[vi] = dp + dn * sideThickness;
					verts[vi + numMainVerts] = dp;

					float v = (p.z - noseV0) * noseVScale;
					float uo = j * segOMappingScale + segOMappingOfs;
					float ui = (numSegs - j) * segIMappingScale + segIMappingOfs;
					if (v > 0 && v < 1) {
						float us = 1 - v;
						uo = (uo - oCenter) * us + oCenter;
						ui = (ui - iCenter) * us + iCenter;
					}

					uv[vi].Set(uo, p.z);
					uv[vi + numMainVerts].Set(ui, p.z);

					norm[vi] = dn;
					norm[vi + numMainVerts] = -dn;
					tang[vi].Set(-d.z, 0, d.x, 0);
					tang[vi + numMainVerts].Set(d.z, 0, -d.x, 0);
				}
			}

			// side strip vertices
			float stripScale = Mathf.Abs(stripMapping.y - stripMapping.x) / (sideThickness * mappingScale.y);

			vi = numMainVerts * 2;
			float o = 0;
			for (int i = 0; i < shape.Length; ++i, vi += 2) {
				int si = i * (numSegs + 1);

				var d = dirs[0];
				verts[vi] = verts[si];
				uv[vi].Set(stripU0, o);
				norm[vi].Set(d.z, 0, -d.x);

				verts[vi + 1] = verts[si + numMainVerts];
				uv[vi + 1].Set(stripU1, o);
				norm[vi + 1] = norm[vi];
				tang[vi] = tang[vi + 1] = (verts[vi + 1] - verts[vi]).normalized;

				if (i + 1 < shape.Length) o += ((Vector2)shape[i + 1] - (Vector2)shape[i]).magnitude * stripScale;
			}

			vi += numSideVerts - 2;
			for (int i = shape.Length - 1; i >= 0; --i, vi -= 2) {
				int si = i * (numSegs + 1) + numSegs;
				if (i == shape.Length - 1 && inlineHeight <= 0) si = numMainVerts - 1;

				var d = dirs[numSegs];
				verts[vi] = verts[si];
				uv[vi].Set(stripU0, o);
				norm[vi].Set(-d.z, 0, d.x);

				verts[vi + 1] = verts[si + numMainVerts];
				uv[vi + 1].Set(stripU1, o);
				norm[vi + 1] = norm[vi];
				tang[vi] = tang[vi + 1] = (verts[vi + 1] - verts[vi]).normalized;

				if (i > 0) o += ((Vector2)shape[i] - (Vector2)shape[i - 1]).magnitude * stripScale;
			}

			// ring vertices
			vi = numMainVerts * 2 + numSideVerts * 2;
			o = 0;
			for (int j = numSegs; j >= 0; --j, vi += 2, o += ringSegLen * stripScale) {
				verts[vi] = verts[j];
				uv[vi].Set(stripU0, o);
				norm[vi] = -Vector3.up;

				verts[vi + 1] = verts[j + numMainVerts];
				uv[vi + 1].Set(stripU1, o);
				norm[vi + 1] = -Vector3.up;
				tang[vi] = tang[vi + 1] = (verts[vi + 1] - verts[vi]).normalized;
			}

			if (inlineHeight > 0) {
				// top ring vertices
				o = 0;
				int si = (shape.Length - 1) * (numSegs + 1);
				for (int j = 0; j <= numSegs; ++j, vi += 2, o += topRingSegLen * stripScale) {
					verts[vi] = verts[si + j];
					uv[vi].Set(stripU0, o);
					norm[vi] = Vector3.up;

					verts[vi + 1] = verts[si + j + numMainVerts];
					uv[vi + 1].Set(stripU1, o);
					norm[vi + 1] = Vector3.up;
					tang[vi] = tang[vi + 1] = (verts[vi + 1] - verts[vi]).normalized;
				}
			}

			// set vertex data to mesh
			for (int i = 0; i < totalVerts; ++i) tang[i].w = 1;
			m.vertices = verts;
			m.uv = uv;
			m.normals = norm;
			m.tangents = tang;

			m.uv2 = null;
			m.colors32 = null;

			var tri = new int[totalFaces * 3];

			// main faces
			vi = 0;
			int ti1 = 0, ti2 = numMainFaces * 3;
			for (int i = 0; i < shape.Length - (inlineHeight <= 0 ? 2 : 1); ++i, ++vi) {
				p = shape[i];
				for (int j = 0; j < numSegs; ++j, ++vi) {
					tri[ti1++] = vi;
					tri[ti1++] = vi + 1 + numSegs + 1;
					tri[ti1++] = vi + 1;

					tri[ti1++] = vi;
					tri[ti1++] = vi + numSegs + 1;
					tri[ti1++] = vi + 1 + numSegs + 1;

					tri[ti2++] = numMainVerts + vi;
					tri[ti2++] = numMainVerts + vi + 1;
					tri[ti2++] = numMainVerts + vi + 1 + numSegs + 1;

					tri[ti2++] = numMainVerts + vi;
					tri[ti2++] = numMainVerts + vi + 1 + numSegs + 1;
					tri[ti2++] = numMainVerts + vi + numSegs + 1;
				}
			}

			if (inlineHeight <= 0) {
				// main tip faces
				for (int j = 0; j < numSegs; ++j, ++vi) {
					tri[ti1++] = vi;
					tri[ti1++] = numMainVerts - 1;
					tri[ti1++] = vi + 1;

					tri[ti2++] = numMainVerts + vi;
					tri[ti2++] = numMainVerts + vi + 1;
					tri[ti2++] = numMainVerts + numMainVerts - 1;
				}
			}

			// side strip faces
			vi = numMainVerts * 2;
			ti1 = numMainFaces * 2 * 3;
			ti2 = ti1 + numSideFaces * 3;
			for (int i = 0; i < shape.Length - 1; ++i, vi += 2) {
				tri[ti1++] = vi;
				tri[ti1++] = vi + 1;
				tri[ti1++] = vi + 3;

				tri[ti1++] = vi;
				tri[ti1++] = vi + 3;
				tri[ti1++] = vi + 2;

				tri[ti2++] = numSideVerts + vi;
				tri[ti2++] = numSideVerts + vi + 3;
				tri[ti2++] = numSideVerts + vi + 1;

				tri[ti2++] = numSideVerts + vi;
				tri[ti2++] = numSideVerts + vi + 2;
				tri[ti2++] = numSideVerts + vi + 3;
			}

			// ring faces
			vi = numMainVerts * 2 + numSideVerts * 2;
			ti1 = (numMainFaces + numSideFaces) * 2 * 3;
			for (int j = 0; j < numSegs; ++j, vi += 2) {
				tri[ti1++] = vi;
				tri[ti1++] = vi + 1;
				tri[ti1++] = vi + 3;

				tri[ti1++] = vi;
				tri[ti1++] = vi + 3;
				tri[ti1++] = vi + 2;
			}

			if (inlineHeight > 0) {
				// top ring faces
				vi += 2;
				for (int j = 0; j < numSegs; ++j, vi += 2) {
					tri[ti1++] = vi;
					tri[ti1++] = vi + 1;
					tri[ti1++] = vi + 3;

					tri[ti1++] = vi;
					tri[ti1++] = vi + 3;
					tri[ti1++] = vi + 2;
				}
			}

			m.triangles = tri;

			if (!HighLogic.LoadedSceneIsEditor) m.Optimize();

			StartCoroutine(PFUtils.updateDragCubeCoroutine(part, 1));
		}
	}


	//ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ//


} // namespace

