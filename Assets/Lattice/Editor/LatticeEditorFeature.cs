using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lattice.Editor
{
	public static class LatticeEditorFeature
	{
		private static bool _initialised = false;

		/// <summary>
		/// Sets up the modifiers as part of the editor loop.
		/// This is called after assembly reloads.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void Initialise()
		{
			if (_initialised) return;

			LatticeFeature.Initialise();
			EditorApplication.quitting += Cleanup;

			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			EditorApplication.playModeStateChanged += OnStateChanged;
			EditorSceneManager.sceneSaved += OnSceneSaved;
			EditorSceneManager.sceneSaving += OnSceneSaving;

			_initialised = true;
		}

		/// <summary>
		/// Cleans up all related systems.
		/// This is called when the editor closes.
		/// </summary>
		private static void Cleanup()
		{
			if (!_initialised) return;

			LatticeFeature.Cleanup();
			EditorApplication.quitting -= Cleanup;

			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			EditorApplication.playModeStateChanged -= OnStateChanged;
			EditorSceneManager.sceneSaved -= OnSceneSaved;
			EditorSceneManager.sceneSaving -= OnSceneSaving;

			_initialised = false;
		}

		/// <summary>
		/// Called before assembly reloads. Cleans up and disables <see cref="LatticeFeature"/>.
		/// </summary>
		private static void OnBeforeAssemblyReload()
		{
			LatticeFeature.Cleanup();
		}

		/// <summary>
		/// Initialises or cleans up depending if entering or exiting edit mode.
		/// </summary>
		private static void OnStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				ResetMeshes();
				LatticeFeature.Cleanup();
			}
			else if (state == PlayModeStateChange.EnteredEditMode)
			{
				LatticeFeature.Initialise();
				ApplyMeshes();
			}
		}

		/// <summary>
		/// Changes all meshes to lattice version after saving.
		/// </summary>
		private static void OnSceneSaved(Scene scene)
		{
			ApplyMeshes();
		}

		/// <summary>
		/// Changes all meshes to non-lattice version before saving.
		/// </summary>
		private static void OnSceneSaving(Scene scene, string path)
		{
			ResetMeshes();
		}

		/// <summary>
		/// Resets meshes back to non lattice version.
		/// </summary>
		private static void ResetMeshes()
		{
			LatticeModifier[] components = Object.FindObjectsOfType<LatticeModifier>();

			for (int i = 0; i < components.Length; i++)
			{
				if (components[i].isActiveAndEnabled) components[i].ResetMesh();
			}
		}

		/// <summary>
		/// Applies lattice version meshes.
		/// </summary>
		private static void ApplyMeshes()
		{
			LatticeModifier[] components = Object.FindObjectsOfType<LatticeModifier>();

			for (int i = 0; i < components.Length; i++)
			{
				if (components[i].isActiveAndEnabled) components[i].ApplyMesh();
			}
		}
	}
}
