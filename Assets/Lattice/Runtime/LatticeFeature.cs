using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

using static Lattice.LatticeShaderProperties;

namespace Lattice
{
	public static class LatticeFeature
	{
		/// <summary>
		/// Hardcoded max number of lattice handles supported. Can be changed.
		/// </summary>
		public const int MaxHandles = 1024;

		private static bool _init = false;

		private static ComputeShader _compute;
		private static uint _computeGroupSize;

		private static ComputeBuffer _latticeBuffer;
		private static readonly int[] _latticeResolution = new int[3];
		private static readonly List<LatticeModifier> _modifiers = new();
		private static readonly List<SkinnedLatticeModifier> _skinnedModifiers = new();

		/// <summary>
		/// Enqueues a mesh to be deformed this frame
		/// </summary>
		public static void Enqueue(LatticeModifier modifier)
		{
			_modifiers.Add(modifier);
		}

		/// <summary>
		/// Enqueues a skinned mesh to be deformed this frame
		/// </summary>
		public static void Enqueue(SkinnedLatticeModifier modifier)
		{
			_modifiers.Add(modifier);
			_skinnedModifiers.Add(modifier);
		}

		/// <summary>
		/// Sets up the modifiers as part of the player loop
		/// </summary>
		[RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		private static void Initialize()
		{
			if (_init) return;

			_compute = Resources.Load<ComputeShader>("Shaders/LatticeCompute");

			if (_compute == null) return;

#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
			UnityEditor.EditorApplication.playModeStateChanged += OnStateChanged;

			Application.quitting += Reset;
			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnQuitting;
			UnityEditor.EditorApplication.quitting += OnQuitting;
#else
			Application.quitting += OnQuitting;
#endif

			_init = true;

			// Create the buffer for storing lattice information
			_latticeBuffer = new(MaxHandles, 3 * sizeof(float));

			// Setup compute
			_compute.GetKernelThreadGroupSizes(0, out _computeGroupSize, out uint _, out uint _);
			_compute.SetBuffer(0, LatticeBufferId, _latticeBuffer);

			// Update the player loop to include lattice modifier
			var loop = PlayerLoop.GetCurrentPlayerLoop();

			int postLateUpdateIndex = Array.FindIndex(loop.subSystemList, dd => dd.type == typeof(PostLateUpdate));
			var postLateUpdate = loop.subSystemList[postLateUpdateIndex];

			var postLateSystems = postLateUpdate.subSystemList.ToList();
			var skinned = postLateSystems.FindIndex(dd => dd.type == typeof(PostLateUpdate.UpdateAllSkinnedMeshes));

			postLateSystems.Insert(skinned, new()
			{
				updateDelegate = ApplyModifiers,
				type = typeof(LatticeFeature)
			});			

			postLateSystems.Insert(skinned + 2, new()
			{
				updateDelegate = ApplySkinnedModifiers,
				type = typeof(LatticeFeature)
			});

			postLateUpdate.subSystemList = postLateSystems.ToArray();
			loop.subSystemList[postLateUpdateIndex] = postLateUpdate;

			PlayerLoop.SetPlayerLoop(loop);
		}

		private static void OnQuitting()
		{
#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
			UnityEditor.EditorApplication.playModeStateChanged -= OnStateChanged;

			Application.quitting -= Reset;
			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnQuitting;
			UnityEditor.EditorApplication.quitting -= OnQuitting;
#else
			Application.quitting -= OnQuitting;
#endif

			// Release the lattice buffer
			_latticeBuffer?.Release();
			_latticeBuffer = null;
			
			// Clear existing modifiers
			_modifiers.Clear();
			_skinnedModifiers.Clear();

			// Remove lattice modifier from player loop
			var loop = PlayerLoop.GetCurrentPlayerLoop();

			int postLateUpdateIndex = Array.FindIndex(loop.subSystemList, dd => dd.type == typeof(PostLateUpdate));
			var postLateUpdate = loop.subSystemList[postLateUpdateIndex];

			var postLateSystems = postLateUpdate.subSystemList.ToList();
			postLateSystems.RemoveAll(dd => dd.type == typeof(LatticeFeature));

			postLateUpdate.subSystemList = postLateSystems.ToArray();
			loop.subSystemList[postLateUpdateIndex] = postLateUpdate;	

			PlayerLoop.SetPlayerLoop(loop);

			_init = false;
		}

		private static void Reset()
		{
			// Refresh modifier system
			OnQuitting();
			Initialize();
		}

		private static void ApplyModifiers()
		{
			if (_modifiers.Count == 0 || _latticeBuffer == null) return;

			// Setup compute - only needed here in development
			// These two calls are only here because editing the compute shader will cause
			// the asset to refresh and these values will be lost.
			_compute.GetKernelThreadGroupSizes(0, out _computeGroupSize, out uint _, out uint _);
			_compute.SetBuffer(0, LatticeBufferId, _latticeBuffer);

			// Get command buffer
			CommandBuffer cmd = CommandBufferPool.Get("Lattice Modifier");

			// Apply all modifiers
			for (int i = 0; i < _modifiers.Count; i++)
			{
				ApplyModifier(cmd, _modifiers[i]);
			}
			
			// Execute
			Graphics.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);

			// Clear modifier queue
			_modifiers.Clear();
		}

		

		private static void SetMeshInfo(CommandBuffer cmd, MeshInfo info)
		{
			cmd.SetComputeIntParam(_compute, VertexCountId,    info.VertexCount);
			cmd.SetComputeIntParam(_compute, BufferStrideId,   info.BufferStride);
			cmd.SetComputeIntParam(_compute, PositionOffsetId, info.PositionOffset);
			cmd.SetComputeIntParam(_compute, NormalOffsetId,   info.NormalOffset);
			cmd.SetComputeIntParam(_compute, TangentOffsetId,  info.TangentOffset);
			cmd.SetComputeIntParam(_compute, StretchOffsetId,  info.StretchOffset);
		}

		private static void ApplyModifier(CommandBuffer cmd, LatticeModifier modifier)
		{
			if (modifier == null || !modifier.IsValid) return;

			// Enable or disable high quality deformations
			cmd.SetKeyword(HighQualityKeyword, modifier.HighQuality);

			// Copy original buffer back onto vertex buffer
			cmd.CopyBuffer(modifier.CopyBuffer, modifier.VertexBuffer);
			
			// Set vertex buffer
			cmd.SetComputeBufferParam(_compute, 0, VertexBufferId, modifier.VertexBuffer);

			// Setup mesh info
			MeshInfo info = modifier.MeshInfo;
			SetMeshInfo(cmd, info);
			
			// Apply lattices
			List<Lattice> lattices = modifier.Lattices;
			for (int i = 0; i < lattices.Count; i++)
			{
				Lattice lattice = lattices[i];

				// Set lattice parameters
				Matrix4x4 objectToLattice = lattice.transform.worldToLocalMatrix * modifier.LocalToWorld;
				Matrix4x4 latticeToObject = objectToLattice.inverse;

				cmd.SetComputeMatrixParam(_compute, ObjectToLatticeId, objectToLattice);
				cmd.SetComputeMatrixParam(_compute, LatticeToObjectId, latticeToObject);

				_latticeResolution[0] = lattice.Resolution.x;
				_latticeResolution[1] = lattice.Resolution.y;
				_latticeResolution[2] = lattice.Resolution.z;
				cmd.SetComputeIntParams(_compute, LatticeResolutionId, _latticeResolution);

				// Set lattice offsets
				cmd.SetBufferData(_latticeBuffer, lattice.Offsets);

				// Apply lattice
				cmd.DispatchCompute(_compute, 0, info.VertexCount / (int)_computeGroupSize + 1, 1, 1);
			}
		}

		private static void ApplySkinnedModifier(CommandBuffer cmd, SkinnedLatticeModifier modifier)
		{
			if (modifier == null) return;


		}

		private static void ApplySkinnedModifiers()
		{
			//if (_skinnedModifiers.Count == 0 || _latticeBuffer == null) return;

			//CommandBuffer cmd = CommandBufferPool.Get("Skinned Lattice");

			//for (int i = 0; i < _skinnedModifiers.Count; i++)
			//{
			//	_skinnedModifiers[i].ExecuteSkinned(cmd, Compute, _latticeBuffer);
			//}

			//Graphics.ExecuteCommandBuffer(cmd);
			//CommandBufferPool.Release(cmd);

			//_skinnedModifiers.Clear();
		}

#if UNITY_EDITOR
		private static void OnStateChanged(UnityEditor.PlayModeStateChange state)
		{
			if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
			{
				LatticeModifier[] components = GameObject.FindObjectsOfType<LatticeModifier>();

				for (int i = 0; i < components.Length; i++)
				{
					if (components[i].isActiveAndEnabled) components[i].ResetMesh();
				}
			}
		}

		private static void OnSceneSaved(Scene scene)
		{
			LatticeModifier[] components = GameObject.FindObjectsOfType<LatticeModifier>();

			for (int i = 0; i < components.Length; i++)
			{
				if (components[i].isActiveAndEnabled) components[i].ApplyMesh();
			}
		}

		private static void OnSceneSaving(Scene scene, string path)
		{
			LatticeModifier[] components = GameObject.FindObjectsOfType<LatticeModifier>();

			for (int i = 0; i < components.Length; i++)
			{
				if (components[i].isActiveAndEnabled) components[i].ResetMesh();
			}
		}
#endif
	}
}