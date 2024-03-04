using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Heath.Lattice.Editor
{
	[System.Serializable]
	internal class LatticeSettings : ScriptableObject
	{
		public Gradient gradient;
		public float lineThickness;
		public float lineThicknessSquish;
		public float lineThicknessStretch;

		public void Reset()
		{
			gradient = new Gradient()
			{
				colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(new Color32(0, 69, 255, 255), 0f),
					new GradientColorKey(new Color32(69, 174, 212, 255), 0.25f),
					new GradientColorKey(new Color32(185, 185, 185, 255), 0.5f),
					new GradientColorKey(new Color32(233, 157, 76, 255), 0.75f),
					new GradientColorKey(new Color32(255, 0, 0, 255), 1f),
				}
			};

			lineThickness = 1f;
			lineThicknessSquish = 8f;
			lineThicknessStretch = 4f;

		}

		private void OnValidate()
		{
			
		}

		public static Gradient Gradient => LatticeSettingsProvider.Settings.gradient;
		public static float LineThickness => LatticeSettingsProvider.Settings.lineThickness;
		public static float LineThicknessSquish => LatticeSettingsProvider.Settings.lineThicknessSquish;
		public static float LineThicknessStretch => LatticeSettingsProvider.Settings.lineThicknessStretch;

	}

	internal class LatticeSettingsProvider : SettingsProvider
	{
		private const string PreferencesPath = "Preferences/Lattice";
		private const string SettingsKey = "Lattice/Settings";
		private static readonly string[] Keywords = new string[] { "Lattice" };

		private static LatticeSettings _settings;
		private static SerializedObject _serializedSettings;

		internal static LatticeSettings Settings
		{
			get
			{
				if (_settings == null)
				{
					string settingsString = EditorPrefs.GetString(SettingsKey, string.Empty);
					_settings = string.IsNullOrEmpty(settingsString) ? GetDefaultSettings() : DeserializeSettings(settingsString);
				}
				return _settings;
			}
		}

		private static SerializedObject SerializedSettings => _serializedSettings ??= new(Settings);

		public LatticeSettingsProvider() : base(PreferencesPath, SettingsScope.User, Keywords) { }

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new LatticeSettingsProvider();
		}

		public override void OnGUI(string searchContext)
		{
			SerializedSettings.Update();
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings.gradient)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings.lineThickness)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings.lineThicknessSquish)));
			EditorGUILayout.PropertyField(SerializedSettings.FindProperty(nameof(LatticeSettings.lineThicknessStretch)));

			SerializedSettings.ApplyModifiedProperties();

			if (GUILayout.Button("Reset to Defaults"))
			{
				Undo.RecordObject(Settings, "Reset Lattice Settings");
				EditorUtility.CopySerialized(GetDefaultSettings(), Settings);
			}
		}

		public override void OnDeactivate()
		{
			string serializedSettings = SerializeSettings(Settings);
			EditorPrefs.SetString(SettingsKey, serializedSettings);
		}

		private static LatticeSettings CreateSettings()
		{
			var settings = ScriptableObject.CreateInstance<LatticeSettings>();
			settings.name = "Lattice Settings";
			return settings;
		}

		private static LatticeSettings GetDefaultSettings()
		{
			var settings = CreateSettings();
			settings.Reset();
			return settings;
		}

		private static LatticeSettings DeserializeSettings(string serializedSettings)
		{
			var settings = GetDefaultSettings();
			EditorJsonUtility.FromJsonOverwrite(serializedSettings, settings);
			return settings;
		}

		private static string SerializeSettings(LatticeSettings settings) 
		{
			return EditorJsonUtility.ToJson(settings, false);
		}
	}
}
