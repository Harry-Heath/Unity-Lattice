using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Lattice
{
	public static class LatticeFeature
	{
		private const int MaxHandles = 1024;

		private static bool _init = false;

		private static ComputeShader _compute;
		private static ComputeBuffer _latticeBuffer;
		private static readonly List<LatticeModifier> _modifiers = new();
		private static readonly List<SkinnedLatticeModifier> _skinnedModifiers = new();

		/// <summary>
		/// The compute shader used to calculate lattice deformations
		/// </summary>
		public static ComputeShader Compute
		{
			get
			{
				if (_compute == null)
				{
					_compute = Resources.Load<ComputeShader>("Shaders/LatticeCompute");
				}
				return _compute;
			}
		}

		/// <summary>
		/// Enqueues a lattice modifier
		/// </summary>
		public static void Enqueue(LatticeModifier modifier)
		{
			_modifiers.Add(modifier);
		}

		/// <summary>
		/// Enqueues a skinned lattice modifer
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

			CommandBuffer cmd = CommandBufferPool.Get("Lattice");

			for (int i = 0; i < _modifiers.Count; i++)
			{
				_modifiers[i].Execute(cmd, Compute, _latticeBuffer);
			}

			Graphics.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);

			_modifiers.Clear();
		}

		private static void ApplySkinnedModifiers()
		{
			if (_skinnedModifiers.Count == 0 || _latticeBuffer == null) return;

			CommandBuffer cmd = CommandBufferPool.Get("Skinned Lattice");

			for (int i = 0; i < _skinnedModifiers.Count; i++)
			{
				_skinnedModifiers[i].ExecuteSkinned(cmd, Compute, _latticeBuffer);
			}

			Graphics.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);

			_skinnedModifiers.Clear();
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