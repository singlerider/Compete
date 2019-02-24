using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour {

	private GameObject _cam;
	private GameObject menu;

	// Use this for initialization
	void Awake () {
		_cam = GameObject.Find ("/Main Camera");
		menu = GameObject.Find ("/Menu");
		menu.transform.position = _cam.transform.position + _cam.transform.forward * 2.5f;
		menu.transform.rotation = _cam.transform.rotation;
	}

	// Update is called once per frame
	void Update () {
		float speed = Time.deltaTime * 5f;

		Vector3 pos = _cam.transform.position + _cam.transform.forward * 1.0f;
		menu.transform.position = Vector3.SlerpUnclamped (menu.transform.position, pos, speed);

		Quaternion rot = Quaternion.LookRotation (menu.transform.position - _cam.transform.position);
		menu.transform.rotation = Quaternion.Slerp (menu.transform.rotation, rot, speed);
	}
}