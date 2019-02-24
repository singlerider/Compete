﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DartCollider : MonoBehaviour {

	private Rigidbody dartRB;
	private AudioSource dartAudio;
	// Use this for initialization
	void Start () {
		dartRB = this.GetComponent<Rigidbody>();
		dartAudio = this.GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	private void OnCollisionEnter(Collision other) {
		dartRB.isKinematic = true;
		dartAudio.Play();
		print("Playsound");
	}
}
