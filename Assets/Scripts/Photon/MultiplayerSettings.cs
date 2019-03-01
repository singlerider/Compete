﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerSettings : MonoBehaviour {

	public static MultiplayerSettings multiplayerSettings;
	
	public bool delayStart;
	public int maxPlayers;

	public int menuScene;
	public int multiplayerScene;
	// Use this for initialization
	void Awake() {
		if (MultiplayerSettings.multiplayerSettings == null) {
			MultiplayerSettings.multiplayerSettings = this;
		} else {
			if (MultiplayerSettings.multiplayerSettings != this) {
				Destroy(this.gameObject);
			}
		}
		DontDestroyOnLoad(this.gameObject);
	}
}
