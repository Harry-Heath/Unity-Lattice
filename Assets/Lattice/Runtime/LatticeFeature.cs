using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace Heath.Lattice
{
	public static class LatticeFeature
	{
		private static bool _init = false;
		private static ComputeShader _compute;
		private static readonly List<LatticeModifier> _modifiers = new();
		private static readonly List<SkinnedLatticeModifier> _skinnedModifiers = new();

		private static ComputeBuffer _latticeBuffer;

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

		[RuntimeInitializeOnLoadMethod]
		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			if (_init) return;

			Debug.Log($"Initialize -> isPlaying:{Application.isPlaying}");

			EditorSceneManager.sceneSaving += OnSceneSaving;
			EditorSceneManager.sceneSaved += OnSceneSaved;
			EditorApplication.playModeStateChanged += OnStateChanged;

			if (!Application.isEditor)
			{
				Application.quitting += OnQuitting;
			}
			else
			{
				Application.quitting += Reset;
				AssemblyReloadEvents.beforeAssemblyReload += OnQuitting;
				EditorApplication.quitting += OnQuitting;
			}

			_init = true;
			_latticeBuffer = new(1024, 3 * sizeof(float));

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
		
		private static void OnStateChanged(PlayModeStateChange obj)
		{
			if (obj == PlayModeStateChange.ExitingEditMode)
			{
				LatticeModifier[] components = GameObject.FindObjectsOfType<LatticeModifier>();

				for (int i = 0; i < components.Length; i++)
				{
					if (components[i].isActiveAndEnabled) components[i].ResetMesh();
				}
			}
		}

		private static void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
		{
			LatticeModifier[] components = GameObject.FindObjectsOfType<LatticeModifier>();

			for (int i = 0; i < components.Length; i++)
			{
				if (components[i].isActiveAndEnabled) components[i].ApplyMesh();
			}
		}

		private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
		{
			LatticeModifier[] components = GameObject.FindObjectsOfType<LatticeModifier>();

			for (int i = 0; i < components.Length; i++)
			{
				if (components[i].isActiveAndEnabled) components[i].ResetMesh();
			}
		}

		private static void OnQuitting()
		{
			Debug.Log($"OnQuitting -> isPlaying:{Application.isPlaying}");//

			EditorSceneManager.sceneSaving -= OnSceneSaving;
			EditorSceneManager.sceneSaved -= OnSceneSaved;
			EditorApplication.playModeStateChanged -= OnStateChanged;

			if (!Application.isEditor)
			{
				Application.quitting -= OnQuitting;
			}
			else
			{
				Application.quitting -= Reset;
				AssemblyReloadEvents.beforeAssemblyReload -= OnQuitting;
				EditorApplication.quitting -= OnQuitting;
			}

			_latticeBuffer?.Release();
			_latticeBuffer = null;

			_modifiers.Clear();
			_skinnedModifiers.Clear();

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
			OnQuitting();
			Initialize();
		}

		private static void ApplyModifiers()
		{
			if (_modifiers.Count == 0 || _latticeBuffer == null) return;

			CommandBuffer cmd = GetCommandBuffer("Lattice");

			for (int i = 0; i < _modifiers.Count; i++)
			{
				_modifiers[i].Execute(cmd, Compute, _latticeBuffer);
			}

			Graphics.ExecuteCommandBuffer(cmd);
			ReleaseCommandBuffer(cmd);

			_modifiers.Clear();
		}

		private static void ApplySkinnedModifiers()
		{
			if (_skinnedModifiers.Count == 0 || _latticeBuffer == null) return;

			CommandBuffer cmd = GetCommandBuffer("Skinned Lattice");

			for (int i = 0; i < _skinnedModifiers.Count; i++)
			{
				_skinnedModifiers[i].ExecuteSkinned(cmd, Compute, _latticeBuffer);
			}

			Graphics.ExecuteCommandBuffer(cmd);
			ReleaseCommandBuffer(cmd);

			_skinnedModifiers.Clear();
		}

		public static void Enqueue(LatticeModifier modifier)
		{
			_modifiers.Add(modifier);
		}

		public static void Enqueue(SkinnedLatticeModifier modifier)
		{
			_modifiers.Add(modifier);
			_skinnedModifiers.Add(modifier);
		}

		private static CommandBuffer GetCommandBuffer(string name)
		{
			return CommandBufferPool.Get(name);
		}

		private static void ReleaseCommandBuffer(CommandBuffer cmd)
		{
			CommandBufferPool.Release(cmd);
		}
	}
}