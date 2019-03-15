using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallHitDetector : MonoBehaviour {
	//private Vector3 objectSize;

	private GameObject ball;
	private float targetSize = 0.05f;
	private float sizeChange = 0.002f;
	private float originalSize = 0.04f;
	private void Start () {
		ball = this.gameObject;
	}
	private void Update () {
		//
	}
	// Use this for initialization
	private void OnTriggerEnter (Collider col) {
		//print (this.gameObject.name);
		BowlingManager.ballColor = this.gameObject.name;

		//objectSize = this.gameObject.transform.localScale;

		if (ball.transform.localScale.x <= targetSize) {
			print ("Growing");
			ball.transform.localScale += new Vector3 (sizeChange, sizeChange, sizeChange);
		}

	}
	private void OnTriggerExit (Collider col) {
		BowlingManager.ballColor = "N/A";
		while (ball.transform.localScale.x >= originalSize) {
			ball.transform.localScale -= new Vector3 (sizeChange, sizeChange, sizeChange);
		}
	}
}