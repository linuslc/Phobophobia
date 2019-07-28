/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"ActionNavMesh.cs"
 * 
 *	Changes any of the following scene parameters: NavMesh, Default PlayerStart, Sorting Map, Tint Map, Cutscene On Load, and Cutscene On Start. When the NavMesh is a Polygon Collider, this Action can also be used to add or remove holes from it.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionNavMesh : Action
	{

		public int constantID = 0;
		public int parameterID = -1;

		public int replaceConstantID = 0;
		public int replaceParameterID = -1;

		public NavigationMesh newNavMesh;
		public SortingMap sortingMap;
		public PlayerStart playerStart;
		public Cutscene cutscene;
		public TintMap tintMap;
		public SceneSetting sceneSetting = SceneSetting.DefaultNavMesh;

		public ChangeNavMeshMethod changeNavMeshMethod = ChangeNavMeshMethod.ChangeNavMesh;
		public InvAction holeAction;

		public PolygonCollider2D hole;
		public PolygonCollider2D replaceHole;

		private SceneSettings sceneSettings;

		private NavigationMesh runtimeNewNavMesh;
		private PolygonCollider2D runtimeHole;
		private PolygonCollider2D runtimeReplaceHole;
		private PlayerStart runtimePlayerStart;
		private SortingMap runtimeSortingMap;
		private TintMap runtimeTintMap;
		private Cutscene runtimeCutscene;


		public ActionNavMesh ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Scene;
			title = "Change setting";
			description = "Changes any of the following scene parameters: NavMesh, Default PlayerStart, Sorting Map, Tint Map, Cutscene On Load, and Cutscene On Start. When the NavMesh is a Polygon Collider, this Action can also be used to add or remove holes from it.";
		}


		override public void AssignValues (List<ActionParameter> parameters)
		{
			if (sceneSettings == null) return;

			switch (sceneSetting)
			{
				case SceneSetting.DefaultNavMesh:
					if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.PolygonCollider && changeNavMeshMethod == ChangeNavMeshMethod.ChangeNumberOfHoles)
					{
						runtimeHole = AssignFile <PolygonCollider2D> (parameters, parameterID, constantID, hole);
						runtimeReplaceHole = AssignFile <PolygonCollider2D> (parameters, replaceParameterID, replaceConstantID, replaceHole);
						runtimeNewNavMesh = null;
					}
					else
					{
						runtimeHole = null;
						runtimeReplaceHole = null;
						runtimeNewNavMesh = AssignFile <NavigationMesh> (parameters, parameterID, constantID, newNavMesh);
					}
					break;

				case SceneSetting.DefaultPlayerStart:
					runtimePlayerStart = AssignFile <PlayerStart> (parameters, parameterID, constantID, playerStart);
					break;

				case SceneSetting.SortingMap:
					runtimeSortingMap = AssignFile <SortingMap> (parameters, parameterID, constantID, sortingMap);
					break;

				case SceneSetting.TintMap:
					runtimeTintMap = AssignFile <TintMap> (parameters, parameterID, constantID, tintMap);
					break;
					
				case SceneSetting.OnLoadCutscene:
				case SceneSetting.OnStartCutscene:
					runtimeCutscene = AssignFile <Cutscene> (parameters, parameterID, constantID, cutscene);
					break;
			}
		}


		override public void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				sceneSettings = UnityVersionHandler.GetSceneSettingsOfGameObject (actionList.gameObject);
			}
			if (sceneSettings == null)
			{
				sceneSettings = KickStarter.sceneSettings;
			}
		}
		
		
		override public float Run ()
		{
			if (sceneSetting == SceneSetting.DefaultNavMesh)
			{
				if (sceneSettings.navigationMethod == AC_NavigationMethod.PolygonCollider && changeNavMeshMethod == ChangeNavMeshMethod.ChangeNumberOfHoles)
				{
					if (runtimeHole != null)
					{
						NavigationMesh currentNavMesh = sceneSettings.navMesh;

						switch (holeAction)
						{
							case InvAction.Add:
								currentNavMesh.AddHole (runtimeHole);
								break;

							case InvAction.Remove:
								currentNavMesh.RemoveHole (runtimeHole);
								break;

							case InvAction.Replace:
								currentNavMesh.AddHole (runtimeHole);
								currentNavMesh.RemoveHole (runtimeReplaceHole);
								break;
						}
					}
				}
				else if (runtimeNewNavMesh != null)
				{
					NavigationMesh oldNavMesh = sceneSettings.navMesh;
					oldNavMesh.TurnOff ();
					runtimeNewNavMesh.TurnOn ();
					sceneSettings.navMesh = runtimeNewNavMesh;

					// Bugfix: Need to cycle this otherwise weight caching doesn't always work
					runtimeNewNavMesh.TurnOff ();
					runtimeNewNavMesh.TurnOn ();

					if (runtimeNewNavMesh.GetComponent <ConstantID>() == null)
					{
						ACDebug.LogWarning ("Warning: Changing to new NavMesh with no ConstantID - change will not be recognised by saved games.", runtimeNewNavMesh);
					}
				}

				// Recalculate pathfinding characters
				foreach (Char _character in KickStarter.stateHandler.Characters)
				{
					_character.RecalculateActivePathfind ();
				}
			}
			else if (sceneSetting == SceneSetting.DefaultPlayerStart && runtimePlayerStart != null)
			{
				sceneSettings.defaultPlayerStart = runtimePlayerStart;

				if (runtimePlayerStart.GetComponent <ConstantID>() == null)
				{
					ACDebug.LogWarning ("Warning: Changing to new default PlayerStart with no ConstantID - change will not be recognised by saved games.", runtimePlayerStart);
				}
			}
			else if (sceneSetting == SceneSetting.SortingMap && runtimeSortingMap != null)
			{
				sceneSettings.sortingMap = runtimeSortingMap;
				sceneSettings.UpdateAllSortingMaps ();

				if (runtimeSortingMap.GetComponent <ConstantID>() == null)
				{
					ACDebug.LogWarning ("Warning: Changing to new SortingMap with no ConstantID - change will not be recognised by saved games.", runtimeSortingMap);
				}
			}
			else if (sceneSetting == SceneSetting.TintMap && runtimeTintMap != null)
			{
				sceneSettings.tintMap = runtimeTintMap;
				
				// Reset all FollowSortingMap components
				FollowTintMap[] followTintMaps = FindObjectsOfType (typeof (FollowTintMap)) as FollowTintMap[];
				foreach (FollowTintMap followTintMap in followTintMaps)
				{
					followTintMap.ResetTintMap ();
				}
				
				if (runtimeTintMap.GetComponent <ConstantID>() == null)
				{
					ACDebug.LogWarning ("Warning: Changing to new TintMap with no ConstantID - change will not be recognised by saved games.", runtimeTintMap);
				}
			}
			else if (sceneSetting == SceneSetting.OnLoadCutscene && runtimeCutscene != null)
			{
				sceneSettings.cutsceneOnLoad = runtimeCutscene;

				if (sceneSettings.actionListSource == ActionListSource.AssetFile)
				{
					ACDebug.LogWarning ("Warning: As the Scene Manager relies on asset files for its cutscenes, changes made with the 'Scene: Change setting' Action will not be felt.");
				}
				else if (runtimeCutscene.GetComponent <ConstantID>() == null)
				{
					ACDebug.LogWarning ("Warning: Changing to Cutscene On Load with no ConstantID - change will not be recognised by saved games.", runtimeCutscene);
				}
			}
			else if (sceneSetting == SceneSetting.OnStartCutscene && runtimeCutscene != null)
			{
				sceneSettings.cutsceneOnStart = runtimeCutscene;

				if (sceneSettings.actionListSource == ActionListSource.AssetFile)
				{
					ACDebug.LogWarning ("Warning: As the Scene Manager relies on asset files for its cutscenes, changes made with the 'Scene: Change setting' Action will not be felt.");
				}
				else if (runtimeCutscene.GetComponent <ConstantID>() == null)
				{
					ACDebug.LogWarning ("Warning: Changing to Cutscene On Start with no ConstantID - change will not be recognised by saved games.", runtimeCutscene);
				}
			}
			
			return 0f;
		}
		

		#if UNITY_EDITOR

		override public void ShowGUI (List<ActionParameter> parameters)
		{
			if (sceneSettings == null)
			{
				EditorGUILayout.HelpBox ("No 'Scene Settings' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
				AfterRunningOption ();
				return;
			}

			sceneSetting = (SceneSetting) EditorGUILayout.EnumPopup ("Scene setting to change:", sceneSetting);

			if (sceneSetting == SceneSetting.DefaultNavMesh)
			{
				if (sceneSettings.navigationMethod == AC_NavigationMethod.meshCollider || sceneSettings.navigationMethod == AC_NavigationMethod.PolygonCollider)
				{
					if (sceneSettings.navigationMethod == AC_NavigationMethod.PolygonCollider)
					{
						changeNavMeshMethod = (ChangeNavMeshMethod) EditorGUILayout.EnumPopup ("Change NavMesh method:", changeNavMeshMethod);
					}

					if (sceneSettings.navigationMethod == AC_NavigationMethod.meshCollider || changeNavMeshMethod == ChangeNavMeshMethod.ChangeNavMesh)
					{
						parameterID = Action.ChooseParameterGUI ("New NavMesh:", parameters, parameterID, ParameterType.GameObject);
						if (parameterID >= 0)
						{
							constantID = 0;
							newNavMesh = null;
						}
						else
						{
							newNavMesh = (NavigationMesh) EditorGUILayout.ObjectField ("New NavMesh:", newNavMesh, typeof (NavigationMesh), true);
							
							constantID = FieldToID <NavigationMesh> (newNavMesh, constantID);
							newNavMesh = IDToField <NavigationMesh> (newNavMesh, constantID, false);
						}
					}
					else if (changeNavMeshMethod == ChangeNavMeshMethod.ChangeNumberOfHoles)
					{
						holeAction = (InvAction) EditorGUILayout.EnumPopup ("Add or remove hole:", holeAction);
						string _label = "Hole to add:";
						if (holeAction == InvAction.Remove)
						{
							_label = "Hole to remove:";
						}

						parameterID = Action.ChooseParameterGUI (_label, parameters, parameterID, ParameterType.GameObject);
						if (parameterID >= 0)
						{
							constantID = 0;
							hole = null;
						}
						else
						{
							hole = (PolygonCollider2D) EditorGUILayout.ObjectField (_label, hole, typeof (PolygonCollider2D), true);
							
							constantID = FieldToID <PolygonCollider2D> (hole, constantID);
							hole = IDToField <PolygonCollider2D> (hole, constantID, false);
						}

						if (holeAction == InvAction.Replace)
						{
							replaceParameterID = Action.ChooseParameterGUI ("Hole to remove:", parameters, replaceParameterID, ParameterType.GameObject);
							if (replaceParameterID >= 0)
							{
								replaceConstantID = 0;
								replaceHole = null;
							}
							else
							{
								replaceHole = (PolygonCollider2D) EditorGUILayout.ObjectField ("Hole to remove:", replaceHole, typeof (PolygonCollider2D), true);
								
								replaceConstantID = FieldToID <PolygonCollider2D> (replaceHole, replaceConstantID);
								replaceHole = IDToField <PolygonCollider2D> (replaceHole, replaceConstantID, false);
							}
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("This action is not compatible with the Unity Navigation pathfinding method, as set in the Scene Manager.", MessageType.Warning);
				}
			}
			else if (sceneSetting == SceneSetting.DefaultPlayerStart)
			{
				parameterID = Action.ChooseParameterGUI ("New default PlayerStart:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					playerStart = null;
				}
				else
				{
					playerStart = (PlayerStart) EditorGUILayout.ObjectField ("New default PlayerStart:", playerStart, typeof (PlayerStart), true);
					
					constantID = FieldToID <PlayerStart> (playerStart, constantID);
					playerStart = IDToField <PlayerStart> (playerStart, constantID, false);
				}
			}
			else if (sceneSetting == SceneSetting.SortingMap)
			{
				parameterID = Action.ChooseParameterGUI ("New SortingMap:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					sortingMap = null;
				}
				else
				{
					sortingMap = (SortingMap) EditorGUILayout.ObjectField ("New SortingMap:", sortingMap, typeof (SortingMap), true);
					
					constantID = FieldToID <SortingMap> (sortingMap, constantID);
					sortingMap = IDToField <SortingMap> (sortingMap, constantID, false);
				}
			}
			else if (sceneSetting == SceneSetting.TintMap)
			{
				parameterID = Action.ChooseParameterGUI ("New TintMap:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					tintMap = null;
				}
				else
				{
					tintMap = (TintMap) EditorGUILayout.ObjectField ("New TintMap:", tintMap, typeof (TintMap), true);
					
					constantID = FieldToID <TintMap> (tintMap, constantID);
					tintMap = IDToField <TintMap> (tintMap, constantID, false);
				}
			}
			else if (sceneSetting == SceneSetting.OnLoadCutscene)
			{
				parameterID = Action.ChooseParameterGUI ("New OnLoad cutscene:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					cutscene = null;
				}
				else
				{
					cutscene = (Cutscene) EditorGUILayout.ObjectField ("New OnLoad cutscene:", cutscene, typeof (Cutscene), true);
					
					constantID = FieldToID <Cutscene> (cutscene, constantID);
					cutscene = IDToField <Cutscene> (cutscene, constantID, false);
				}
			}
			else if (sceneSetting == SceneSetting.OnStartCutscene)
			{
				parameterID = Action.ChooseParameterGUI ("New OnStart cutscene:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					cutscene = null;
				}
				else
				{
					cutscene = (Cutscene) EditorGUILayout.ObjectField ("New OnStart cutscene:", cutscene, typeof (Cutscene), true);
					
					constantID = FieldToID <Cutscene> (cutscene, constantID);
					cutscene = IDToField <Cutscene> (cutscene, constantID, false);
				}
			}
			
			AfterRunningOption ();
		}


		override public void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (sceneSetting == SceneSetting.DefaultNavMesh)
			{
				if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.PolygonCollider && changeNavMeshMethod == ChangeNavMeshMethod.ChangeNumberOfHoles)
				{
					if (saveScriptsToo)
					{
						if (KickStarter.sceneSettings != null && KickStarter.sceneSettings != null)
						{
							AddSaveScript <RememberNavMesh2D> (KickStarter.sceneSettings.navMesh);
						}
						AddSaveScript <ConstantID> (hole);
						AddSaveScript <ConstantID> (replaceHole);
					}
					AssignConstantID <PolygonCollider2D> (hole, constantID, parameterID);
					AssignConstantID <PolygonCollider2D> (replaceHole, replaceConstantID, replaceParameterID);
				}
				else
				{
					if (saveScriptsToo)
					{
						AddSaveScript <ConstantID> (newNavMesh);
					}
					AssignConstantID <NavigationMesh> (newNavMesh, constantID, parameterID);
				}
			}
			else if (sceneSetting == SceneSetting.DefaultPlayerStart)
			{
				if (saveScriptsToo)
				{
					AddSaveScript <ConstantID> (playerStart);
				}
				AssignConstantID <PlayerStart> (playerStart, constantID, parameterID);
			}
			else if (sceneSetting == SceneSetting.SortingMap)
			{
				if (saveScriptsToo)
				{
					AddSaveScript <ConstantID> (sortingMap);
				}
				AssignConstantID <SortingMap> (sortingMap, constantID, parameterID);
			}
			else if (sceneSetting == SceneSetting.TintMap)
			{
				if (saveScriptsToo)
				{
					AddSaveScript <ConstantID> (tintMap);
				}
				AssignConstantID <TintMap> (tintMap, constantID, parameterID);
			}
			else if (sceneSetting == SceneSetting.OnLoadCutscene || sceneSetting == SceneSetting.OnStartCutscene)
			{
				AssignConstantID <Cutscene> (cutscene, constantID, parameterID);
			}
		}
		
		
		override public string SetLabel ()
		{
			return sceneSetting.ToString ();
		}

		#endif
		
	}

}