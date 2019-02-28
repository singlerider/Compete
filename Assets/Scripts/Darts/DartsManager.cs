using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.MagicLeap;
public class DartsManager : MonoBehaviour {
	public enum holdState {
		none,
		dart,
		dartboard
	}
	public static holdState holding = holdState.none;
	private MLInputController controller;
	public GameObject mainCam, control, dartPrefab, dartboardHolder, menu, modifierMenu, tutorialMenu, dartMenu, dartboard;
	public Transform dartHolder;
	public static GameObject menuControl;
	private GameObject dart;
	public Material transparent, activeMat;
	public Material[] dartMats;
	public LineRenderer laserLineRenderer;
	private Vector3 endPosition, forcePerSecond;
	private float timeHold = 3.0f, totalObjs = 0, objLimit = 50;
	private Controller checkController;
	private bool setHand = false, holdingDart = false, tutorialActive = true, noGravity = false, dartMenuOpened = false, holdingDartMenu = true, tutorialBumperPressed, tutorialHomePressed, tutorialMenuOpened = false, movingDartboard = true;
	private static bool menuClosed = false, menuOpened = false;
	public static bool lockedDartboard = false;
	List<Vector3> Deltas = new List<Vector3> ();

	// Use this for initialization
	void Start () {
		CheckNewUser();
		MLInput.Start ();
		controller = MLInput.GetController (MLInput.Hand.Left);
		MLInput.OnControllerButtonDown += OnButtonDown;

		// Initialize both line points at Vector3.zero
		Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
		laserLineRenderer.SetPositions (initLaserPositions);

		menuControl = GameObject.Find ("ObjectMenu");
		checkController = control.GetComponentInChildren<Controller> ();
	}
	private void OnDestroy () {
		MLInput.Stop ();
	}

	// Update is called once per frame
	void Update () {
		control.transform.position = controller.Position;
		control.transform.rotation = controller.Orientation;
		if (tutorialActive == false) {
			SetLine ();
			PlaceObject ();
		} else {
			if ((controller.Touch1Active || controller.TriggerValue >= 0.9f || tutorialBumperPressed || tutorialHomePressed) && tutorialMenuOpened == false) {
				laserLineRenderer.material = activeMat;
				CheckNewUser();
			}
		}
		if (controller.Touch1Active) {
			if (menuClosed == false) {
				menuControl.SetActive (true);
			}
			if (setHand == false) {
				setHand = true;

				menuControl.transform.position = controller.Position;
				menuControl.transform.rotation = mainCam.transform.rotation;
			}
		}
		if (controller.Touch1Active == false) {
			menuClosed = false;
			setHand = false;
			menuControl.SetActive (false);
		}
		if (checkController.bumperTimer.getTime () >= timeHold) {
			ClearAllObjects ();
		}
		if (controller.TriggerValue <= 0.2f && tutorialMenuOpened == true) {
			tutorialMenuOpened = false;
		}
	}
	private void SetLine () {
		RaycastHit rayHit;
		Vector3 heading = control.transform.forward;

		// Set the origin of the line to the controller's position, because the first position does not dynamically change
		laserLineRenderer.SetPosition (0, controller.Position);
		if (Physics.Raycast (controller.Position, heading, out rayHit, 10.0f)) {
			endPosition = controller.Position + (control.transform.forward * rayHit.distance);
			laserLineRenderer.SetPosition (1, endPosition);
			if (rayHit.transform.gameObject.name == "Home" && controller.TriggerValue >= 0.9f) {
				SceneManager.LoadScene("Main", LoadSceneMode.Single);
			} else if (rayHit.transform.gameObject.name == "ChangeDart" && controller.TriggerValue >= 0.9f) {
				dart = Instantiate(dartPrefab, new Vector3(100, 100, 100), controller.Orientation, dartHolder);
				dartMenu.transform.position = mainCam.transform.position + (mainCam.transform.forward * 1.5f);
				dartMenu.transform.rotation = mainCam.transform.rotation;
				dartMenu.SetActive(true);
				menuClosed = true;
				menu.SetActive(false);
				dartMenuOpened = true;
				holdingDartMenu = true;
			} else if (rayHit.transform.gameObject.name == "Modifiers" && controller.TriggerValue >= 0.9f) {
				modifierMenu.SetActive(true);
				menu.SetActive(false);
				menuClosed = true;
			}  else if (rayHit.transform.gameObject.name == "Tutorial" && controller.TriggerValue >= 0.9f) {
				menuClosed = true;
				menuOpened = false;
				menu.SetActive(false);
				holding = holdState.none;
				tutorialActive = true;
				tutorialMenuOpened = true;
				tutorialMenu.SetActive(true);
				tutorialBumperPressed = false;
				tutorialHomePressed = false;
				laserLineRenderer.material = transparent;
				PlayerPrefs.SetInt("hasPlayedDarts", 0);
				CheckNewUser();
			} else if (rayHit.transform.gameObject.name == "NoGravity" && controller.TriggerValue >= 0.9f) {
				if (noGravity) {
					noGravity = false;
				} else {
					noGravity = true;
				}
				modifierMenu.SetActive(false);
			}
			if (!holdingDartMenu) {
				DartColorLoader.GetDartColor(rayHit.transform.gameObject.name, controller, dartMenu, dartMenuOpened, holdingDartMenu, dart, dartMats);
			} else if (holdingDartMenu && controller.TriggerValue <= 0.2f) {
				holdingDartMenu = false;
			}
		} else {
			endPosition = controller.Position + (control.transform.forward * 3.0f);
			laserLineRenderer.SetPosition (1, endPosition);
		}
	}
	private void PlaceObject () {
		if (holding == holdState.dart) {
			movingDartboard = true;
			laserLineRenderer.material = transparent;
			if (controller.TriggerValue >= 0.9f) {
				if (holdingDart) {
					HoldingDart();
				} else {
					SpawnObject();
					holdingDart = true;
				}
			} else if (controller.TriggerValue <= 0.2f && holdingDart) {
				holdingDart = false;
				var rigidbody = dart.transform.GetChild (0).gameObject.GetComponent<Rigidbody> ();
				rigidbody.useGravity = true;
				rigidbody.velocity = Vector3.zero;
				rigidbody.velocity = forcePerSecond;
			}
		} else if (holding == holdState.dartboard) {
			print("hmm");
			dartboardHolder.SetActive(true);
			if (controller.TriggerValue >= 0.9f) {
				if (!lockedDartboard) {
					print("Should see dartboard");
					movingDartboard = false;
					var dartboardCollider = dartboard.GetComponent<BoxCollider> ();
					dartboardCollider.enabled = true;
					lockedDartboard = true;
				}
			} else if (controller.TriggerValue <= 0.2f) {
				if (movingDartboard) {
					dartboardHolder.transform.position = endPosition;
					dartboardHolder.transform.rotation = Quaternion.LookRotation (-mainCam.transform.up, -mainCam.transform.forward);
					lockedDartboard = false;
				} else {
					laserLineRenderer.material = transparent;
				}
			}
		} else if (holding == holdState.none && tutorialMenuOpened == false) {
			laserLineRenderer.material = activeMat;
		}
	}
	private void HoldingDart () {
		var oldPosition = dart.transform.position;
		var newPosition = controller.Position;

		var delta = newPosition - oldPosition;
		if (Deltas.Count == 15) {
			Deltas.RemoveAt (0);
		}
		Deltas.Add (delta);
		Vector3 toAverage = Vector3.zero;
		foreach (var toAdd in Deltas) {
			toAverage += toAdd;
		}
		toAverage /= Deltas.Count;
		var forcePerSecondAvg = toAverage * 400;
		forcePerSecond = forcePerSecondAvg;
		dart.transform.position = controller.Position;
		dart.transform.rotation = controller.Orientation;
	}

	private void SpawnObject() {
		if (totalObjs < objLimit) {
			if (!noGravity) {
				if (holding == holdState.dart) {
					dart = Instantiate (dartPrefab, controller.Position, controller.Orientation, dartHolder);
					Transform dartChild = dart.gameObject.transform.GetChild(0);
					Renderer dartRender = dartChild.GetComponent<Renderer>();
					dartRender.material = dartMats[PlayerPrefs.GetInt("dartColorInt", 0)];
				}
			}
		}
	}

	void OnButtonDown (byte controller_id, MLInputControllerButton button) {
		holding = holdState.none;
		if (button == MLInputControllerButton.HomeTap && tutorialActive == true) {
			tutorialHomePressed = true;
		} else if (button == MLInputControllerButton.Bumper && tutorialActive == true) {
			tutorialBumperPressed = true;
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == false) {
			menuOpened = true;
			menu.SetActive (true);
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == true) {
			menuOpened = false;
			menu.SetActive (true);
		}
	}
	private void ClearAllObjects () {
		foreach (Transform child in dartHolder) {
			GameObject.Destroy(child.gameObject);
		}
	}
	public static void CloseMenu () {
		print ("Close the menu");
		menuClosed = true;
		menuControl.SetActive (false);
	}
	private void GetCount() {
		foreach (Transform dartObj in dartHolder) {
			totalObjs += 1;
		}
	}
	private void CheckNewUser() {
		if (PlayerPrefs.GetInt("hasPlayedDarts") == 1) {
			print("Played");
			tutorialActive = false;
			laserLineRenderer.material = activeMat;
			tutorialMenu.SetActive(false);
		} else {
			print("Not Played");
			tutorialMenu.SetActive(true);
			PlayerPrefs.SetInt("hasPlayedDarts", 1);
		}
	}
}