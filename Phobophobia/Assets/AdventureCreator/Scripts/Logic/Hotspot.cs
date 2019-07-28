/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"Hotspot.cs"
 * 
 *	This script handles all the possible
 *	interactions on both hotspots and NPCs.
 * 
 */

using UnityEngine;
using System.Collections.Generic;


namespace AC
{

	/**
	 * This component provides the player with a region of space in the scene that can be interacted with.
	 * Data for each interaction is stored within the Button class, and this component stores them in Lists.
	 * The number of interactions, and how exactly they are triggered, are determined in SettingsManager.
	 */
	[AddComponentMenu("Adventure Creator/Hotspots/Hotspot")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_hotspot.html")]
	#endif
	public class Hotspot : MonoBehaviour, ITranslatable
	{

		/** If True, then a Gizmo will be drawn in the Scene window at the Hotspots's position */
		public bool showInEditor = true;
		/** The source of the commands that are run when an option is chosen (InScene, AssetFile, CustomScript) */	
		public AC.InteractionSource interactionSource;
		/** If assigned, then the Hotspot will only be interactive when the assigned _Camera is active */
		public _Camera limitToCamera = null;
		/** If assigned, then the Hotspot will only be interactive when the player is within this Trigger Collider's boundary */
		public InteractiveBoundary interactiveBoundary = null;

		/** The display name, if not the GameObject's name */
		public string hotspotName;
		/** The translation ID number of the Hotspot's name, as generated by SpeechManager */
		public int lineID = -1;
		/** The Highlight component that controls any highlighting effects associated with the Hotspot */
		public Highlight highlight;
		/** The Marker that the player can optionally automatically walk to before an Interaction runs */
		public Marker walkToMarker;
		/** A Transform that represents the centre of the Hotspot, if it is not physically at the same point as the Hotspot's GameObject itself */
		public Transform centrePoint;

		/** If True, then the Hotspot can have 'Use" interactions */
		public bool provideUseInteraction;
		/** No longer used by Adventure Creator, but kept so that older projects can be upgraded */
		public Button useButton = new Button();

		/** A List of all available 'Use' interactions */
		public List<Button> useButtons = new List<Button>();
		/** If True, then clicking the Hotspot will run the Hotspot's first interaction in useButtons, regardless of the interactionMethod chosen in SettingsManager */
		public bool oneClick = false;

		/** If True, then the Hotspot can have an 'Examine' interaction, if interactionMethod = AC_InteractionMethod.ContextSensitive in SettingsManager */
		public bool provideLookInteraction;
		/** The 'Examine' interaction, if interactionMethod = AC_InteractionMethod.ContextSensitive in SettingsManager */
		public Button lookButton = new Button();

		/** If True, then the Hotspot can have 'Inventory' interactions */
		public bool provideInvInteraction;
		/** A List of all available 'Inventory' interactions */
		public List<Button> invButtons = new List<Button>();
	
		/** If True, then the Hotspot can have an unhandled 'Inventory' interaction */
		public bool provideUnhandledInvInteraction;
		/** The unhandled 'Inventory' interaction, which will be run if the player uses an inventory item on the Hotspot, and it is not handled within invButtons */
		public Button unhandledInvButton = new Button();

		/** If True, then a Gizmo may be drawn in the Scene window at the Hotspots's position, if showInEditor = True */
		public bool drawGizmos = true;

		/** The index of the last-active interaction */
		public int lastInteractionIndex = 0;
		/** The translation ID number of the Hotspot's name, if it was changed mid-game */
		public int displayLineID = -1;
		/** The 'Sorting Layer' of the icon's SpriteRenderer, if drawn in World Space */
		public string iconSortingLayer = "";
		/** The 'Order in Layer' of the icon's SpriteRenderer, if drawn in World Space */
		public int iconSortingOrder = 0;

		/** The effect that double-clicking on the Hotspot has, if interactionMethod = AC_InteractionMethod.ContextSensitive in SettingsManager (MakesPlayerRun, TriggersInteractionInstantly) */
		public DoubleClickingHotspot doubleClickingHotspot = DoubleClickingHotspot.MakesPlayerRun;

		/** If True, then the player will turn their head when the Hotspot is selected (if SettingsManager's playerFacesHotspots = True) */
		public bool playerTurnsHead = true;

		private Collider _collider;
		private Collider2D _collider2D;
		private bool isOn = true;
		private float iconAlpha = 0;
		private UnityEngine.Sprite iconSprite = null;
		private SpriteRenderer iconRenderer = null;
		private CursorIcon mainIcon;

		private LerpUtils.FloatLerp iconAlphaLerp = new LerpUtils.FloatLerp (true);

		private float manualShowIconSpeed = 5f;
		private bool manuallyShowIcon = false;
		private bool tooFarAway = false;

		
		private void Awake ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				UpgradeSelf ();
			}
			
			if (GetComponent <Collider>())
			{
				_collider = GetComponent <Collider>();
			}
			else if (GetComponent <Collider2D>())
			{
				_collider2D = GetComponent <Collider2D>();
			}

			lastInteractionIndex = FindFirstEnabledInteraction ();
			displayLineID = lineID;
		}


		private void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		private void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		private void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}


		/**
		 * <summary>Runs the Hotspot's 'Examine' interaction, if one is defined.</summary>
		 */
		public void RunExamineInteraction ()
		{
			if (lookButton != null)
			{
				KickStarter.playerInteraction.ExamineHotspot (this);
			}
		}


		/**
		 * <summary>Runs one of the Hotspot's 'Use' interactions.</summary>
		 * <param name = "iconID">The ID number of the CursorIcon associated with the Button. If no number is supplied, the first enabled Use interaction will be run.</param>
		 */
		public void RunUseInteraction (int iconID = -1)
		{
			if (useButtons == null || useButtons.Count == 0)
			{
				return;
			}

			iconID = Mathf.Max (-1, iconID);
			KickStarter.playerInteraction.UseHotspot (this, iconID);
		}


		/**
		 * <summary>Runs one of the Hotspot's 'Inventory' interactions.</summary>
		 * <param name = "invID">The ID number of the InvItem associated with the Button. If no number is supplied, that of the currently-selected inventory item will be used.</param>
		 */
		public void RunInventoryInteraction (int invID = -1)
		{
			if (invID < 0)
			{
				if (KickStarter.runtimeInventory.SelectedItem != null)
				{
					invID = KickStarter.runtimeInventory.SelectedItem.id;
				}
				else
				{
					return;
				}
			}

			KickStarter.playerInteraction.UseInventoryOnHotspot (this, invID);
		}


		/**
		 * <summary>Runs one of the Hotspot's 'Inventory' interactions.</summary>
		 * <param name = "invItem">The InvItem associated with the Button. If no item is supplied, that of the currently-selected inventory item will be used.</param>
		 */
		public void RunInventoryInteraction (InvItem invItem = null)
		{
			int invID = -1;

			if (invItem != null)
			{
				invID = invItem.id;
			}
			else
			{
				if (KickStarter.runtimeInventory.SelectedItem != null)
				{
					invID = KickStarter.runtimeInventory.SelectedItem.id;
				}
				else
				{
					return;
				}
			}

			KickStarter.playerInteraction.UseInventoryOnHotspot (this, invID);
		}


		private void FindFirstInteractionIndex ()
		{
			lastInteractionIndex = 0;

			foreach (Button button in useButtons)
			{
				if (!button.isDisabled)
				{
					lastInteractionIndex = useButtons.IndexOf (button);
					return;
				}
			}
		}


		/**
		 * <summary>Highlights the Hotspot based on the mouse cursor's proximity.</summary>
		 * <param name = "isGameplay">If True, then it is during gameplay, and the highlight effect wil work</param>
		 */
		public void SetProximity (bool isGameplay)
		{
			if (highlight != null)
			{
				if (!isGameplay || !IsOn ())
				{
					highlight.SetMinHighlight (0f);
				}
				else
				{
					float amount = (GetIconScreenPosition () - KickStarter.playerInput.GetMousePosition ()).magnitude / AdvGame.GetMainGameViewDistance ();

					if (amount < 0f)
					{
						amount = 0f;
					}
					else if (amount > 1f)
					{
						amount = 1f;
					}
				
					highlight.SetMinHighlight (1f - (amount * KickStarter.settingsManager.highlightProximityFactor));
				}
			}
		}
		

		/**
		 * <summary>Upgrades the Hotspot from a previous version of Adventure Creator.</summary>
		 * <returns>True if the upgrade was successful</returns>
		 */
		public bool UpgradeSelf ()
		{
			if (useButton.IsButtonModified ())
			{
				Button newUseButton = new Button ();
				newUseButton.CopyButton (useButton);
				useButtons.Add (newUseButton);
				useButton = new Button ();
				provideUseInteraction = true;

				if (Application.isPlaying)
				{
					ACDebug.Log ("Hotspot '" + gameObject.name + "' has been temporarily upgraded - please view its Inspector when the game ends and save the scene.", gameObject);
				}
				else
				{
					ACDebug.Log ("Upgraded Hotspot '" + gameObject.name + "', please save the scene.", gameObject);
				}

				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Draws an icon at the Hotspot's centre.</summary>
		 * <param name = "inWorldSpace">If True, the icon shall be drawn as a sprite in world space, as opposed to an OnGUI graphic in screen space.</param>
		 */
		public void DrawHotspotIcon (bool inWorldSpace = false)
		{
			if (iconAlpha > 0f)
			{
				if (!KickStarter.mainCamera.IsPointInCamera (GetIconScreenPosition ()))
				{
					return;
				}

				if (inWorldSpace)
				{
					if (iconRenderer == null)
					{
						GameObject iconOb = new GameObject (this.name + " - icon");
						iconRenderer = iconOb.AddComponent <SpriteRenderer>();
						iconOb.transform.localScale = Vector3.one * (25f * KickStarter.settingsManager.hotspotIconSize);

						if (iconSortingLayer != "")
						{
							iconRenderer.GetComponent <SpriteRenderer>().sortingLayerName = iconSortingLayer;
						}
						iconRenderer.GetComponent <SpriteRenderer>().sortingOrder = iconSortingOrder;
					}

					if (KickStarter.settingsManager.hotspotIcon == HotspotIcon.UseIcon)
					{
						GetMainIcon ();
						if (mainIcon != null)
						{
							iconRenderer.sprite = mainIcon.GetSprite ();
						}
					}
					else
					{
						if (iconSprite == null && KickStarter.settingsManager.hotspotIconTexture != null)
						{
							iconSprite = UnityEngine.Sprite.Create (KickStarter.settingsManager.hotspotIconTexture, new Rect (0f, 0f, KickStarter.settingsManager.hotspotIconTexture.width, KickStarter.settingsManager.hotspotIconTexture.height), new Vector2 (0.5f, 0.5f));
						}
						if (iconSprite != iconRenderer.sprite)
						{
							iconRenderer.sprite = iconSprite;
						}
					}

					iconRenderer.transform.position = GetIconPosition ();
					iconRenderer.transform.LookAt (iconRenderer.transform.position + KickStarter.mainCamera.transform.rotation * Vector3.forward, KickStarter.mainCamera.transform.rotation * Vector3.up);
				}
				else
				{
					if (iconRenderer != null)
					{
						GameObject.Destroy (iconRenderer.gameObject);
						iconRenderer = null;
					}

					Color c = GUI.color;
					Color tempColor = c;
					c.a = iconAlpha;
					GUI.color = c;
					
					if (KickStarter.settingsManager.hotspotIcon == HotspotIcon.UseIcon)
					{
						GetMainIcon ();
						if (mainIcon != null)
						{
							mainIcon.Draw (GetIconScreenPosition (), !KickStarter.playerMenus.IsMouseOverInteractionMenu ());
						}
					}
					else if (KickStarter.settingsManager.hotspotIconTexture != null)
					{
						GUI.DrawTexture (AdvGame.GUIBox (GetIconScreenPosition (), KickStarter.settingsManager.hotspotIconSize), KickStarter.settingsManager.hotspotIconTexture, ScaleMode.ScaleToFit, true, 0f);
					}
					
					GUI.color = tempColor;
				}
			}

			if (inWorldSpace && iconRenderer != null)
			{
				Color tempColor = iconRenderer.color;
				tempColor.a = iconAlpha;
				iconRenderer.color = tempColor;
			}
		}


		/**
		 * <summary>Gets the label to display when the cursor is over this Hotspot, with cursor names and active inventory item included if appropriate.</summary>
		 * <param name = "languageNumber">The index number of the language to return the line in, where 0 = the game's original language.</param>
		 * <param name = "cursorID">The ID number of the cursor icon (set in the Cursor Manager) to get the label for. If <0, the active cursor will be used.</param>
		 * <returns>The label to display when the cursor is over this Hotspot, with cursor names and active inventory item included if appropriate.</returns>
		 */
		public string GetFullLabel (int languageNumber = 0, int cursorID = -1)
		{
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && !KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return string.Empty;
			}
			return AdvGame.CombineLanguageString (
							KickStarter.playerInteraction.GetLabelPrefix (this, null, languageNumber, cursorID),
							GetName (languageNumber),
							languageNumber
							);
		}

		
		/**
		 * Recalculates the alpha value of the Hotspot's icon.
		 */
		public void UpdateIcon ()
		{
			CanDisplayHotspotIcon ();
		}


		/**
		 * <summary>Sets the layer of the Hotspot according to whether or not it is within the proximity of a Hotspot detector.</summary>
		 * <param name = "detectHotspot">The DetectHotspots component to check the proximity against</param>
		 */
		public void UpdateProximity (DetectHotspots detectHotspots)
		{
			if (detectHotspots == null) return;

			tooFarAway = !detectHotspots.IsHotspotInTrigger (this);
			if (tooFarAway)
			{
				PlaceOnDistantLayer ();
			}
			else
			{
				PlaceOnHotspotLayer ();
			}
		}


		/**
		 * <summary>Sets the layer of the Hotspot according to whether or not it has a "Use" interaction for the currently-selected cursor</summary>
		 * <returns>True if the Hotspot is on the default layer, False if not</returns>
		 */
		public bool UpdateUnhandledVisibility ()
		{
			if (!HasEnabledUseInteraction (KickStarter.playerCursor.GetSelectedCursorID ()))
			{
				PlaceOnDistantLayer ();
				return false;
			}

			PlaceOnHotspotLayer ();
			return true;
		}


		private void PlaceOnDistantLayer ()
		{
			if (gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer))
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.distantHotspotLayer);
			}
		}


		private void PlaceOnHotspotLayer ()
		{
			if (gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.distantHotspotLayer))
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
			}
		}


		private bool CanDisplayHotspotIcon ()
		{
			if (IsOn ())
			{
				if (Camera.main == null) return false;

				Vector3 direction = (transform.position - Camera.main.transform.position);
				if (Vector3.Angle (direction, Camera.main.transform.forward) > 90f)
				{
					iconAlpha = 0f;
					return false;
				}
				
				if (SceneSettings.CameraPerspective != CameraPerspective.TwoD && KickStarter.settingsManager.occludeIcons)
				{
					// Is icon occluded?
					Ray ray = new Ray (Camera.main.transform.position, GetIconPosition () - Camera.main.transform.position);
					RaycastHit hit;
					if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.hotspotRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer)))
					{
						if (hit.collider.gameObject != this.gameObject)
						{
							iconAlpha = 0f;
							return false;
						}
					}
				}

				if (!KickStarter.stateHandler.IsInGameplay ())
				{
					iconAlpha = 0f;
					return false;
				}
				else if (KickStarter.playerMenus.IsInteractionMenuOn () && KickStarter.settingsManager.hideIconUnderInteractionMenu)
				{
					iconAlpha = iconAlphaLerp.Update (iconAlpha, 0f, 5f);
				}
				else if (KickStarter.settingsManager.hotspotIconDisplay == HotspotIconDisplay.ViaScriptOnly)
				{
					if (manualShowIconSpeed > 0f)
					{
						iconAlpha = iconAlphaLerp.Update (iconAlpha, (manuallyShowIcon) ? 1f : 0f, manualShowIconSpeed);
					}
					else
					{
						iconAlpha = (manuallyShowIcon) ? 1f : 0f;
					}
				}
				else if (KickStarter.settingsManager.hotspotIconDisplay == HotspotIconDisplay.OnlyWhenHighlighting ||
				         KickStarter.settingsManager.hotspotIconDisplay == HotspotIconDisplay.OnlyWhenFlashing)
				{
					if (highlight)
					{
						if (KickStarter.settingsManager.hotspotIconDisplay == HotspotIconDisplay.OnlyWhenHighlighting)
						{
							iconAlpha = highlight.GetHighlightAlpha ();
						}

						else
						{
							iconAlpha = highlight.GetFlashAlpha (iconAlpha);
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot display correct Hotspot Icon alpha on " + name + " because it has no associated Highlight object.", gameObject);
					}
				}
				else if (KickStarter.settingsManager.hotspotIconDisplay == HotspotIconDisplay.Always)
				{
					iconAlpha = 1f;
				}
				else
				{
					iconAlpha = 0f;
				}
				return true;
			}
			else
			{
				iconAlpha = 0f;
				return false;
			}
		}


		/**
		 * <summary>Shows or hides the Hotspot's associated icon, provided that the Settings Manager's hotspotIconDisplay = HotspotIconDisplay.ViaScriptOnly</summary>
		 * <param name = "makeVisible">If True, the icon will be shown. If false, the icon will be hidden</param>
		 * <param name = "speed">The speed at which to show or hide the icon. If <=0, the transition will be instantaneous.</param>
		 */
		public void SetIconVisibility (bool makeVisible, float speed = 5f)
		{
			manuallyShowIcon = makeVisible;
			manualShowIconSpeed = speed;
		}
		

		/**
		 * <summary>Gets the Button that represents the first-available "Use" interaction.</summary>
		 * <returns>The Button that represents the first-available "Use" interaction</returns>
		 */
		public Button GetFirstUseButton ()
		{
			foreach (Button button in useButtons)
			{
				if (button != null && !button.isDisabled)
				{
					return button;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the ID of the icon that represents the first-available "Use" interaction.</summary>
		 * <returns>The ID of the icon that represents the first-available "Use" interaction. If no appropriate interaction is found, -1 is returned</returns>
		 */
		public int GetFirstUseIcon ()
		{
			foreach (Button button in useButtons)
			{
				if (button != null && !button.isDisabled)
				{
					return button.iconID;
				}
			}
			return -1;
		}


		/**
		 * <summary>Gets the Button that represents the first-available "Use" interaction associated with a given interaction icon.</summary>
		 * <param name = "iconID">The ID number of the associated interaction icon, as defined in the Cursor Manager.</param>
		 * <returns>The Button that represents the first-available "Use" interaction associated with a given interaction ID.</returns>
		 */
		public Button GetUseButton (int iconID)
		{
			foreach (Button button in useButtons)
			{
				if (button != null && button.iconID == iconID)
				{
					return button;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the Button that represents the first-available "Inventory" interaction associated with a given inventory item.</summary>
		 * <param name = "invID">The ID number of the associated inventory item, as defined in the Inventory Manager.</param>
		 * <returns>The Button that represents the first-available "Inventory" interaction associated with a given inventory item.</returns>
		 */
		public Button GetInvButton (int invID)
		{
			foreach (Button button in invButtons)
			{
				if (button != null && button.invID == invID)
				{
					return button;
				}
			}
			return null;
		}


		/**
		 * <summary>Checks if a given Button is one of the Hotpspot's invButtons</summary>
		 * <param name = "_button">The Button to check</param>
		 * <returns>True if the Button is one of the Hotspot's invButtons</param>
		 */
		public bool IsInventoryButton (Button _button)
		{
			foreach (Button button in invButtons)
			{
				if (button != null && _button == button)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Gets the index number of the Button that represents the first-available "Use" interaction.</summary>
		 * <returns>The index number of the Button that represents the first-available "Use" interaction</returns>
		 */
		public int FindFirstEnabledInteraction ()
		{
			if (useButtons != null && useButtons.Count > 0)
			{
				for (int i=0; i<useButtons.Count; i++)
				{
					if (!useButtons[i].isDisabled)
					{
						return i;
					}
				}
			}
			return 0;
		}


		/**
		 * <summary>Enables or disables the Hotspot, based on the active camera, if limitToCamera has been assigned.</summary>
		 * <param name = "_limitToCamera">A _Camera that, if matches the limitToCamera variable, will turn the Hotspot on - otherwise the Hotspot will turn off</param>
		 */
		public void LimitToCamera (_Camera _limitToCamera)
		{
			if (limitToCamera != null && _limitToCamera != null)
			{
				if (_limitToCamera == limitToCamera && isOn)
				{
					TurnOn (false);
				}
				else
				{
					TurnOff (false);
				}
			}
		}


		/**
		 * <summary>Enables the Hotspot.</summary>
		 */
		public void TurnOn ()
		{
			TurnOn (true);
		}


		/**
		 * <summary>Enables the Hotspot.</summary>
		 * <param name = "manualSet">If True, then the Hotspot will be considered 'Off" when saving</param>
		 */
		public void TurnOn (bool manualSet)
		{
			if (tooFarAway)
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.distantHotspotLayer);
			}
			else
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
			}

			if (manualSet)
			{
				if (!isOn && KickStarter.eventManager != null)
				{
					KickStarter.eventManager.Call_OnTurnHotspot (this, true);
				}

				isOn = true;

				if (KickStarter.mainCamera != null)
				{
					LimitToCamera (KickStarter.mainCamera.attachedCamera);
				}
			}
		}


		/**
		 * <summary>Disables the Hotspot.</summary>
		 */
		public void TurnOff ()
		{
			TurnOff (true);
		}


		/**
		 * <summary>Disables the Hotspot.</summary>
		 * <param name = "manualSet">If True, then the Hotspot will be considered 'Off" when saving</param>
		 */
		public void TurnOff (bool manualSet)
		{
			gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);

			if (manualSet)
			{
				if (isOn && KickStarter.eventManager != null)
				{
					KickStarter.eventManager.Call_OnTurnHotspot (this, false);
				}

				isOn = false;

				if (KickStarter.player != null && KickStarter.player.hotspotDetector != null)
				{
					KickStarter.player.hotspotDetector.ForceRemoveHotspot (this);
				}
			}
		}
		

		/**
		 * <summary>Checks if the Hotspot is enabled or not.</summary>
		 * <returns>True if the Hotspot is enabled. If the Hotspot is not active only because its limitToCamera is not active, then True will be returned.</returns>
		 */
		public bool IsOn ()
		{
			if (this == null || gameObject == null) return false;

			if (gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer) && !isOn)
			{
				return false;
			}

			return true;
		}


		/**
		 * <summary>Checks if the Player is within the Hotspot's interactableBoundary, if assigned.</summary>
		 * <returns>True if the Player is within the Hotspot's interactableBoundary, if assigned.  If no InteractableBoundary is assigned, or there is no Player, then True will be returned.</returns>
		 */
		public bool PlayerIsWithinBoundary ()
		{
			if (interactiveBoundary == null || KickStarter.player == null)
			{
				return true;
			}

			return interactiveBoundary.PlayerIsPresent;
		}
		

		/**
		 * Selects the Hotspot.
		 */
		public void Select ()
		{
			KickStarter.eventManager.Call_OnChangeHotspot (this, true);

			if (highlight != null && highlight.highlightWhenSelected)
			{
				highlight.HighlightOn ();
			}
		}
		

		/**
		 * De-selects the Hotspot.
		 */
		public void Deselect ()
		{
			KickStarter.eventManager.Call_OnChangeHotspot (this, false);

			if (highlight)
			{
				highlight.HighlightOff ();
			}
		}


		
		/**
		 * De-selects the Hotspot instantly.
		 */
		public void DeselectInstant ()
		{
			KickStarter.eventManager.Call_OnChangeHotspot (this, false);
			
			if (highlight)
			{
				highlight.HighlightOffInstant ();
			}
		}


		/**
		 * <summary>Shows any Menus with appearType = AppearType.OnInteraction, connected to a the Hotspot.</summary>
		 */
		public void ShowInteractionMenus ()
		{
			if (KickStarter.playerMenus != null)
			{
				KickStarter.playerMenus.EnableInteractionMenus (this);
			}
		}
		

		/**
		 * <summary>Checks if oneClick = True, and the Hotspot has at least one "Use" interaction defined.</summary>
		 * <returns>True if oneClick = True, and the Hotspot has at least one "Use" interaction defined.</summmary>
		 */
		public bool IsSingleInteraction ()
		{
			if (oneClick && provideUseInteraction && useButtons != null && GetFirstUseButton () != null)
			{
				return true;
			}
			return false;
		}
		

		#if UNITY_EDITOR

		private void OnDrawGizmos ()
		{
			if (showInEditor)
			{
				DrawGizmos ();
			}
		}
		
		
		private void OnDrawGizmosSelected ()
		{
			DrawGizmos ();
		}


		private void DrawGizmos ()
		{
			if (this.GetComponent <AC.Char>() == null && drawGizmos)
			{
				Color gizmoColor = new Color (1f, 1f, 0f, 0.6f);

				if (GetComponent <PolygonCollider2D>())
				{
					AdvGame.DrawPolygonCollider (transform, GetComponent <PolygonCollider2D>(), gizmoColor);
				}
				else if (GetComponent <MeshCollider>())
				{
					AdvGame.DrawMeshCollider (transform, GetComponent <MeshCollider>().sharedMesh, gizmoColor);
				}
				else if (GetComponent <SphereCollider>())
				{
					AdvGame.DrawSphereCollider (transform, GetComponent <SphereCollider>(), gizmoColor);
				}
				else
				{
					AdvGame.DrawCubeCollider (transform, gizmoColor);
				}
			}
		}


		public int GetInventoryReferences (int invID)
		{
			int numFound = 0;
			foreach (Button invButton in invButtons)
			{
				if (invButton.invID == invID)
				{
					numFound ++;
				}
			}
			return numFound;
		}

		#endif


		/**
		 * <summary>Gets the position of the Hotspot's icon, in Screen Space.</summary>
		 * <returns>The position of the Hotspot's icon, in Screen Space.</returns>
		 */
		public Vector2 GetIconScreenPosition ()
		{
			Vector3 screenPosition = Camera.main.WorldToScreenPoint (GetIconPosition ());
			return new Vector3 (screenPosition.x, screenPosition.y);
		}
		

		/**
		 * <summary>Gets the position of the Hotspot's icon</summary>
		 * <param = "inLocalSpace">If True, the position returned will be relative to the centre of the Hotspot's transform, rather than the scene's origin</param>
		 * <returns>The position of the Hotspot's icon</returns>
		 */
		public Vector3 GetIconPosition (bool inLocalSpace = false)
		{
			Vector3 worldPoint = transform.position;

			if (centrePoint != null)
			{
				if (inLocalSpace)
				{
					return (centrePoint.position - transform.position);
				}
				return centrePoint.position;
			}
			
			if (_collider != null)
			{
				worldPoint = _collider.bounds.center;
			}
			else if (_collider2D != null)
			{
				worldPoint = _collider2D.bounds.center;
			}

			if (inLocalSpace)
			{
				return worldPoint - transform.position;
			}
			return worldPoint;
		}


		/**
		 * Clears the Hotspot's internal 'use' icon, as used when the Hotspot is highlighted.
		 */
		public void ResetMainIcon ()
		{
			mainIcon = null;
		}
		
		
		private void GetMainIcon ()
		{
			if (mainIcon != null)
			{
				return;
			}

			if (KickStarter.cursorManager == null)
			{
				return;
			}
			
			if (provideUseInteraction && useButton != null && useButton.iconID >= 0 && !useButton.isDisabled)
			{
				mainIcon = new CursorIcon ();
				mainIcon.Copy (KickStarter.cursorManager.GetCursorIconFromID (useButton.iconID), true);
				return;
			}
			
			if (provideLookInteraction && lookButton != null && lookButton.iconID >= 0 && !lookButton.isDisabled)
			{
				mainIcon = new CursorIcon ();
				mainIcon.Copy (KickStarter.cursorManager.GetCursorIconFromID (lookButton.iconID), true);
				return;
			}
			
			if (provideUseInteraction && useButtons != null && useButtons.Count > 0)
			{
				for (int i=0; i<useButtons.Count; i++)
				{
					if (!useButtons[i].isDisabled)
					{
						mainIcon = new CursorIcon ();
						mainIcon.Copy (KickStarter.cursorManager.GetCursorIconFromID (useButtons[i].iconID), true);
						return;
					}
				}
			}
			
			return;
		}
		

		/**
		 * <summary>Checks if the Hotspot has at least one "Use" interaction defined.</summary>
		 * <returns>True if the Hotspot has at least one "Use" interaction defined.</returns>
		 */
		public bool HasContextUse ()
		{
			if ((oneClick || KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive) && provideUseInteraction && useButtons != null && GetFirstUseButton () != null)
			{
				return true;
			}
			
			return false;
		}
		

		/**
		 * <summary>Checks if the Hotspot has at least one "Examine" interaction defined.</summary>
		 * <returns>True if the Hotspot has at least one "Examine" interaction defined.</returns>
		 */
		public bool HasContextLook ()
		{
			if (provideLookInteraction && lookButton != null && !lookButton.isDisabled)
			{
				return true;
			}
			
			return false;
		}


		/**
		 * <summary>Gets the next interaction index.</summary>
		 * <param name = "i">The current interaction index</param>
		 * <param name = "numInvInteractions">The number of relevant "Inventory" interactions that match the current cursor</param>
		 * <returns>The next interaction index</returns>
		 */
		public int GetNextInteraction (int i, int numInvInteractions)
		{
			if (i < useButtons.Count)
			{
				if (IsSingleInteraction ())
				{
					i = useButtons.Count;
				}
				else
				{
					i ++;
				}

				while (i<useButtons.Count && useButtons[i].isDisabled)
				{
					i++;
				}

				if (i >= useButtons.Count + numInvInteractions)
				{
					return FindFirstEnabledInteraction ();
				}
				else
				{

					return i;
				}
			}
			else if (i >= useButtons.Count - 1 + numInvInteractions)
			{
				return FindFirstEnabledInteraction ();
			}

			return (i+1);
		}


		private int FindLastEnabledInteraction (int numInvInteractions)
		{
			if (numInvInteractions > 0)
			{
				if (useButtons != null)
				{
					return (useButtons.Count - 1 + numInvInteractions);
				}
				return (numInvInteractions - 1);
			}

			if (useButtons != null && useButtons.Count > 0)
			{
				for (int i=useButtons.Count-1; i>=0; i--)
				{
					if (!useButtons[i].isDisabled)
					{
						return i;
					}
				}
			}
			return 0;
		}


		/**
		 * <summary>Gets the previous interaction index.</summary>
		 * <param name = "i">The current interaction index</param>
		 * <param name = "numInvInteractions">The number of relevant "Inventory" interactions that match the current cursor</param>
		 * <returns>The previous interaction index</returns>
		 */
		public int GetPreviousInteraction (int i, int numInvInteractions)
		{
			if (i > useButtons.Count && numInvInteractions > 0)
			{
				return (i-1);
			}
			else if (i == 0)
			{
				return FindLastEnabledInteraction (numInvInteractions);
			}
			else if (i <= useButtons.Count)
			{
				i --;
				while (i > 0 && useButtons [i].isDisabled)
				{
					i --;
				}

				if (i < 0)
				{
					return FindLastEnabledInteraction (numInvInteractions);
				}
				else
				{
					if (i == 0 && useButtons.Count > 0 && useButtons[0].isDisabled)
					{
						return FindLastEnabledInteraction (numInvInteractions);
					}
					return i;
				}
			}

			return (i-1);
		}


		/**
		 * <summary>Gets the Hotspot's current display name.</summary>
		 * <param name = "languageNumber">The index number of the game's current language</param>
		 * <returns>The Hotspot's current display name</returns>
		 */
		public string GetName (int languageNumber)
		{
			string newName = gameObject.name;
			if (!string.IsNullOrEmpty (hotspotName))
			{
				newName = hotspotName;
			}

			if (languageNumber > 0)
			{
				return KickStarter.runtimeLanguages.GetTranslation (newName, displayLineID, languageNumber);
			}

			return newName;
		}


		/**
		 * <summary>Renames the Hotspot mid-game.</summary>
		 * <param name = "newName">The new name of the Hotspot</param>
		 * <param name = "_lineID">The translation ID number assocated with the new name, as set by SpeechManager</param>
		 */
		public void SetName (string newName, int _lineID)
		{
			hotspotName = newName;

			if (_lineID >= 0)
			{
				displayLineID = _lineID;
			}
			else
			{
				displayLineID = lineID;
			}
		}


		private bool HasEnabledUseInteraction (int _iconID)
		{
			if (_iconID >= 0)
			{
				for (int i=0; i<useButtons.Count; i++)
				{
					if (useButtons[i].iconID == _iconID && !useButtons[i].isDisabled)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if the Hotspot has an active interaction for a given inventory item, or a generic unhandled inventory interaction.</summary>
		 * <param name = "invItem">The inventory item to check for</param>
		 * <returns>True if the Hotspot has an active interaction for the inventory item, or a generic unhandled inventory interaction.</returns>
		 */
		public bool HasInventoryInteraction (InvItem invItem)
		{
			if (invItem != null)
			{
				if (provideUnhandledInvInteraction && unhandledInvButton != null && !unhandledInvButton.isDisabled)
				{
					return true;
				}

				if (provideInvInteraction && invButtons != null && invButtons.Count > 0)
				{
					for (int i=0; i<invButtons.Count; i++)
					{
						if (!invButtons[i].isDisabled && invButtons[i].invID == invItem.id)
						{
							return true;
						}
					}
				}
			}
			return false;
		}


		private int GetNumInteractions (int numInvInteractions)
		{
			int num = 0;
			foreach (Button _button in useButtons)
			{
				if (!_button.isDisabled)
				{
					num ++;
				}
			}
			return (num + numInvInteractions);
		}


		/** ITranslatable implementation */

		public string GetTranslatableString (int index)
		{
			if (!string.IsNullOrEmpty (hotspotName))
			{
				return hotspotName;
			}
			return name;
		}


		public int GetTranslationID (int index)
		{
			return lineID;
		}


		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			return 1;
		}


		public bool HasExistingTranslation (int index)
		{
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			lineID = _lineID;
		}


		public string GetOwner ()
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer ()
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Hotspot;
		}


		public bool CanTranslate (int index)
		{
			return (!string.IsNullOrEmpty (name) || !string.IsNullOrEmpty (hotspotName));
		}

		#endif

	}
	
}