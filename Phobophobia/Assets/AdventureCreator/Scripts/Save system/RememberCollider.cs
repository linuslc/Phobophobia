﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"RememberCollider.cs"
 * 
 *	This script is attached to Colliders in the scene
 *	whose on/off state we wish to save. 
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script is attached to Colliders in the scene whose on/off state you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Collider")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_collider.html")]
	#endif
	public class RememberCollider : Remember
	{

		/** Determines whether the Collider is on or off when the game begins */
		public AC_OnOff startState = AC_OnOff.On;

		private bool loadedData = false;

		
		private void Awake ()
		{
			if (loadedData) return;

			if (KickStarter.settingsManager && GameIsPlaying ())
			{
				bool isOn = (startState == AC_OnOff.On);

				if (GetComponent <Collider>())
				{
					GetComponent <Collider>().enabled = isOn;
				}

				else if (GetComponent <Collider2D>())
				{
					GetComponent <Collider2D>().enabled = isOn;
				}
			}
		}
		

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			ColliderData colliderData = new ColliderData ();

			colliderData.objectID = constantID;
			colliderData.savePrevented = savePrevented;
			colliderData.isOn = false;

			if (GetComponent <Collider>())
			{
				colliderData.isOn = GetComponent <Collider>().enabled;
			}
			else if (GetComponent <Collider2D>())
			{
				colliderData.isOn = GetComponent <Collider2D>().enabled;
			}

			return Serializer.SaveScriptData <ColliderData> (colliderData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			ColliderData data = Serializer.LoadScriptData <ColliderData> (stringData);
			if (data == null)
			{
				loadedData = false;
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (GetComponent <Collider>())
			{
				GetComponent <Collider>().enabled = data.isOn;
			}
			else if (GetComponent <Collider2D>())
			{
				GetComponent <Collider2D>().enabled = data.isOn;
			}

			loadedData = true;
		}

	}


	/**
	 * A data container used by the RememberCollider script.
	 */
	[System.Serializable]
	public class ColliderData : RememberData
	{

		/** True if the Collider is enabled */
		public bool isOn;

		/**
		 * The default Constructor.
		 */
		public ColliderData () { }

	}

}