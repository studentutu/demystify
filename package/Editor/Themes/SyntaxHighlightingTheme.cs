using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Needle.Demystify
{
	[CreateAssetMenu(menuName = "Needle/Demystify/Syntax Highlighting Theme")]
	public class SyntaxHighlightingTheme : ScriptableObject
	{
		public Theme theme = new Theme("New Theme");
	}

	[CustomEditor(typeof(SyntaxHighlightingTheme))]
	public class SyntaxHighlightingThemeEditor : Editor
	{
		private Highlighting previewHighlightingStyle;

		private bool editColors
		{
			get => SessionState.GetBool("DemystifyTheme.EditColors", false);
			set => SessionState.SetBool("DemystifyTheme.EditColors", value);
		}

		private void OnEnable()
		{
			previewHighlightingStyle = DemystifySettings.instance.SyntaxHighlighting;
			if (target is SyntaxHighlightingTheme sh && sh.theme != null)
				sh.theme.Name = target.name;
		}

		public override void OnInspectorGUI()
		{
			var targetTheme = target as SyntaxHighlightingTheme;
			if (!targetTheme) return;
			var theme = targetTheme.theme;

			theme.EnsureEntries();

			serializedObject.Update();

			// Name inspector
			var themeProperty = serializedObject.FindProperty("theme");
			var nameProperty = themeProperty.FindPropertyRelative("Name");

			EditorGUI.BeginChangeCheck();
			using (new EditorGUI.DisabledScope(true))
			{
				nameProperty.stringValue = target.name;
				EditorGUILayout.PropertyField(nameProperty);
			}

			EditorGUILayout.Space();

			editColors = EditorGUILayout.Toggle(new GUIContent("Edit colors in groups", "Enable to edit multiple fields of the same color at once"), editColors);
			if (editColors)
			{
				HandleColorEditing(theme);
				EditorGUILayout.Space(10);
			}

			EditorGUILayout.LabelField("Theme Colors", EditorStyles.boldLabel);
			DemystifySettingsProvider.DrawThemeColorOptions(theme, false);

			EditorGUILayout.Space();
			if (GUILayout.Button("Copy from Active"))
			{
				var currentTheme = DemystifySettings.instance.CurrentTheme;
				targetTheme.theme = currentTheme; 
			}

			if (GUILayout.Button("Activate"))
			{
				DemystifySettings.instance.CurrentTheme = theme;
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo(target, "Edited " + name);
				if (theme == DemystifySettings.instance.CurrentTheme)
					DemystifySettings.instance.UpdateCurrentTheme();
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}

			EditorGUILayout.Space();
			DrawPreview(theme, ref previewHighlightingStyle);
		}

		private static readonly Dictionary<string, string> previewColorDict = new Dictionary<string, string>();
		private static GUIStyle previewStyle;

		private static bool themePreviewFoldout 
		{
			get => SessionState.GetBool("DemystifySyntaxPreviewFoldout", false);
			set => SessionState.SetBool("DemystifySyntaxPreviewFoldout", value);
		}
		
		private static void DrawPreview(Theme theme, ref Highlighting style)
		{
			EditorGUILayout.Space(8);
			themePreviewFoldout = EditorGUILayout.Foldout(themePreviewFoldout, "Theme Preview");
			if(!themePreviewFoldout) return;
			// style = (Highlighting) EditorGUILayout.EnumPopup("Preview Style", style); 
			EditorGUILayout.Space(5);
			
			if(previewStyle == null)
				previewStyle = new GUIStyle(EditorStyles.label) {richText = true, wordWrap = false};
			using (new EditorGUI.DisabledScope(true))
			{
				var settings = DemystifySettings.instance;
				// var currentStyle = settings.SyntaxHighlighting;
				// settings.SyntaxHighlighting = style;
				theme.SetActive(previewColorDict);
				var str = DummyData.SyntaxHighlightVisualization;
				DemystifySettingsProvider.ApplySyntaxHighlightingMultiline(ref str, previewColorDict);;
				// settings.SyntaxHighlighting = currentStyle;
				GUILayout.TextArea(str, previewStyle);
			}
		}

		private static readonly List<(Color color, List<int> matches)> groups = new List<(Color, List<int>)>();
		
		private void HandleColorEditing(Theme theme)
		{
			groups.Clear();
			for (var index = 0; index < theme.Entries.Count; index++)
			{
				var col = theme.Entries[index];
				var group = groups.FirstOrDefault(e => e.color == col.Color);
				if (group.matches == null)
				{
					group = (col.Color, new List<int>());
					groups.Add(group);
				}
				group.matches.Add(index);
			}
			
			EditorGUI.BeginChangeCheck();
			for (var index = 0; index < groups.Count; index++)
			{
				var group = groups[index];
				group.color = EditorGUILayout.ColorField(group.matches.Count.ToString(), group.color);
				groups[index] = group;
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo(target, $"Batch edit {theme.Name} colors");
				foreach (var group in groups)
				{
					foreach (var index in group.matches)
					{
						theme.Entries[index].Color = group.color;
					}
				}
			}
		}
	}
}