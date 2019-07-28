using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	public class ActionFocusWindow : EditorWindow
	{

		private bool isAsset;
		private int selectedIndex;
		private string[] labels;
		private ActionListEditorWindow editorWindow;


		public static void Init (ActionListEditorWindow _editorWindow, List<Action> actions, bool _isAsset)
		{
			ActionFocusWindow window = EditorWindow.GetWindowWithRect <ActionFocusWindow> (new Rect (0, 0, 200, 20), true, "Frame Action", true);

			UnityVersionHandler.SetWindowTitle (window, "Frame Action");
			window.position = new Rect (300, 200, 200, 20);
			window.isAsset = _isAsset;
			window.editorWindow = _editorWindow;

			List<string> labelList = new List<string>();
			for (int i=0; i<actions.Count; i++)
			{
				labelList.Add ("(" + i.ToString () + ") " + actions[i].category + ": " + actions[i].title);
			}
			window.labels = labelList.ToArray ();
		}


		private void OnGUI ()
		{
			GUI.changed = false;
			selectedIndex = EditorGUILayout.Popup (selectedIndex, labels);
			if (GUI.changed)
			{
				editorWindow.FocusOnAction (selectedIndex, isAsset);
			}
		}

	}

}