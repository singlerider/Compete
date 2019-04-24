using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;


public class DartsManager : MonoBehaviour {
	public enum holdState {
		none,
		dart,
		dartboard
	}

	public enum HandPoses {OpenHandBack, Fist, NoPose};
	public HandPoses pose = HandPoses.NoPose;
	public Vector3[] pos;
	private MLHandKeyPose[] _gestures;
	public static holdState holding = holdState.none;
	private MLInputController controller;
	public GameObject mainCam, control, dartPrefab, dartboardHolder, menu, modifierMenu, tutorialMenu, dartMenu, dartboard, deleteLoader, menuCanvas, handCenter;
	public Text dartLimitText;
	public Transform dartHolder, meshHolder;
	public static GameObject menuControl;
	private GameObject dart;
	public Material transparent, activeMat;
	public Material[] dartMats, meshMats;
	public LineRenderer laserLineRenderer;
	public MeshRenderer mesh;
	private Vector3 endPosition, forcePerSecond;
	private float timeHold = 3.0f, totalObjs = 0, objLimit = 20, timeHomePress = 0.5f, timeOfFirstHomePress;
	private Controller checkController;

	public Image loadingImage;

	private bool setHand = false, holdingDart = false, tutorialActive = true, noGravity = false, dartMenuOpened = false, holdingDartMenu = true, tutorialBumperPressed, tutorialHomePressed, movingDartboard = true, settingsOpened = false, occlusionActive = true, tutorialMenuOpened = false, firstHomePressed = false;
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

		MLHands.Start();
		_gestures = new MLHandKeyPose[2];
		_gestures[0] = MLHandKeyPose.OpenHandBack;
		_gestures[1] = MLHandKeyPose.Fist;
		MLHands.KeyPoseManager.EnableKeyPoses(_gestures, true, false);
		pos = new Vector3[1];
	}

	private void OnDestroy () {
		MLInput.Stop ();
        //SceneManager.UnloadSceneAsync("Darts");
		MLHands.Stop();
    }
	private void OnDisable() {
		MLInput.Stop();
		MLHands.Stop();
	}

	private void OnApplicationPause(bool pause) {
		MLInput.Stop();
		MLHands.Stop();
	}

	// Update is called once per frame
	void Update () {

		CheckGestures();
		control.transform.position = controller.Position;
		control.transform.rotation = controller.Orientation;
		if (tutorialActive == false) {
			SetLine ();
			PlaceObject ();
		} else {
			if ((controller.Touch1Active || controller.TriggerValue >= 0.2f || tutorialBumperPressed || tutorialHomePressed) && tutorialMenuOpened == false) {
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
				// Vector3[] zero = new Vector3[2] { Vector3.zero, Vector3.zero };
				// laserLineRenderer.SetPositions (zero);

				menuControl.transform.position = controller.Position;
				menuControl.transform.rotation = mainCam.transform.rotation;
			}
		}
		if (controller.Touch1Active == false) {
			menuClosed = false;
			setHand = false;
			menuControl.SetActive (false);
		}
		if ((checkController.bumperTimer.getTime() >= 0) && (checkController.bumperTimer.getTime() < timeHold)) {
			deleteLoader.SetActive(true);
			float currentTime = checkController.bumperTimer.getTime();
			float percentComplete = currentTime / timeHold;
			loadingImage.fillAmount = percentComplete;
		} else if (checkController.bumperTimer.getTime () >= timeHold) {
			//print("yeeted");
			deleteLoader.SetActive(false);
			ClearAllObjects ();
		} else if (checkController.bumperTimer.getTime() <= 0) {
			//print("deletus feetus");
			deleteLoader.SetActive(false);
		}
		if (controller.TriggerValue <= 0.2f && tutorialMenuOpened == true) {
			tutorialMenuOpened = false;
		}
	}
	private void CheckGestures() {
		if (GetGesture(MLHands.Left, MLHandKeyPose.OpenHandBack)) {
			pose = HandPoses.OpenHandBack;
		} else {
			pose = HandPoses.NoPose;
		}

		if (pose != HandPoses.NoPose) ShowPoints();
	}
	private void ShowPoints() {
		if (!handCenter.activeSelf) {
			handCenter.SetActive(true);
		}
		pos[0] = MLHands.Left.Middle.KeyPoints[0].Position;
		handCenter.transform.position = pos[0];
		handCenter.transform.LookAt(mainCam.transform.position);
	}
	private void SetLine () {
		RaycastHit rayHit;
		Vector3 heading = control.transform.forward;

		// Set the origin of the line to the controller's position, because the first position does not dynamically change
		laserLineRenderer.SetPosition (0, controller.Position);
		if (Physics.Raycast (controller.Position, heading, out rayHit, 10.0f)) {
			endPosition = controller.Position + (control.transform.forward * rayHit.distance);
			laserLineRenderer.SetPosition (1, endPosition);

			if (settingsOpened && controller.TriggerValue <= 0.2f) {
				settingsOpened = false;
			}

			if (rayHit.transform.gameObject.name == "Home" && controller.TriggerValue >= 0.9f) {
				SceneManager.LoadScene("Main", LoadSceneMode.Single);
                SceneManager.UnloadSceneAsync("Darts");
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
				settingsOpened = true;
			} else if (rayHit.transform.gameObject.name == "Tutorial" && controller.TriggerValue >= 0.9f) {
				menuClosed = true;
				menuOpened = false;
				menu.SetActive (false);
				holding = holdState.none;
				tutorialActive = true;
				tutorialMenuOpened = true;
				tutorialMenu.SetActive (true);
				tutorialBumperPressed = false;
				tutorialHomePressed = false;
				laserLineRenderer.material = transparent;
				PlayerPrefs.SetInt ("hasPlayedDarts", 0);
				CheckNewUser ();
			} else if (rayHit.transform.gameObject.name == "NoGravity" && controller.TriggerValue >= 0.9f) {
				if (!settingsOpened) {
					if (noGravity) {
						noGravity = false;
					} else {
						noGravity = true;
					}
					modifierMenu.SetActive(false);
					menuClosed = true;
				}
			} else if (rayHit.transform.gameObject.name == "ShowMesh" && controller.TriggerValue >= 0.9f) {
				if (!settingsOpened) {
					if (occlusionActive) {
						foreach (Transform child in meshHolder) {
							var objectRender = child.GetComponent<MeshRenderer>();
							objectRender.material = meshMats[1];
						}
						mesh.material = meshMats[1];
						occlusionActive = false;
					} else {
						foreach (Transform child in meshHolder) {
							var objectRender = child.GetComponent<MeshRenderer>();
							objectRender.material = meshMats[0];
						}
						mesh.material = meshMats[0];
						occlusionActive = true;
					}
					modifierMenu.SetActive(false);
					menuClosed = true;
				}
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
		if (holding == holdState.dart) {
			laserLineRenderer.SetPosition(0, mainCam.transform.position);
			laserLineRenderer.SetPosition(1, mainCam.transform.position);
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
			
			dartboardHolder.SetActive(true);
			if (controller.TriggerValue >= 0.9f) {
				if (!lockedDartboard) {
					print("Should see dartboard");
					movingDartboard = false;
					var dartboardCollider = dartboard.GetComponent<MeshCollider> ();
					dartboardCollider.enabled = true;
					lockedDartboard = true;
				}
			} else if (controller.TriggerValue <= 0.2f) {
				if (movingDartboard) {
					dartboardHolder.transform.position = endPosition;
					dartboardHolder.transform.rotation = Quaternion.LookRotation (-mainCam.transform.up, -mainCam.transform.forward);
					var dartboardCollider = dartboard.GetComponent<MeshCollider> ();
					dartboardCollider.enabled = false;
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
		var forcePerSecondAvg = toAverage * 250;
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
			} else {
				if (holding == holdState.dart) {
					dart = Instantiate (dartPrefab, controller.Position, controller.Orientation, dartHolder);
					Transform dartChild = dart.gameObject.transform.GetChild(0);
					Renderer dartRender = dartChild.GetComponent<Renderer>();
					Rigidbody dartRB = dartChild.GetComponent<Rigidbody>();
					dartRB.useGravity = false;
					dartRender.material = dartMats[PlayerPrefs.GetInt("dartColorInt", 0)];
				}
			}
		} else {
			GameObject.Destroy(dart);
			print("Too many objects, destroyed last dart");
		}
		// Recount the total number of darts currently in the game to ensure that there are never too many on screen (by objLimit)
		GetCount();
	}

	void OnButtonDown (byte controller_id, MLInputControllerButton button) {
		
		menuCanvas.transform.position = mainCam.transform.position + mainCam.transform.forward * 1.0f;
		menuCanvas.transform.rotation = mainCam.transform.rotation;
		
		holding = holdState.none;
		if (button == MLInputControllerButton.HomeTap && tutorialActive == true) {
			tutorialHomePressed = true;
		} else if (button == MLInputControllerButton.Bumper && tutorialActive == true) {
			tutorialBumperPressed = true;
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == false) {
			laserLineRenderer.material = activeMat;
			menuOpened = true;
			menu.SetActive (true);
			//CenterCam();
			modifierMenu.SetActive(false);
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == true) {
			menuOpened = false;
			menu.SetActive (false);
			modifierMenu.SetActive(false);
		}

		if (button == MLInputControllerButton.HomeTap && firstHomePressed) {
			if (Time.time - timeOfFirstHomePress < timeHomePress) {
				MLInput.Stop();
				MLHands.Stop();
				Debug.Log("DARTS MANAGER: QUITTING APP");
				Application.Quit();
				Debug.Log("DARTS MANAGER: AFTER APP CLOSE");
				timeOfFirstHomePress = 0f;
			}
			firstHomePressed = false;
		} else if (button == MLInputControllerButton.HomeTap && !firstHomePressed) {
			firstHomePressed = true;
			timeOfFirstHomePress = Time.time;
		}
	}
	private void ClearAllObjects () {
		foreach (Transform child in dartHolder) {
			GameObject.Destroy(child.gameObject);
		}
		totalObjs = 0;
		holding = holdState.none;
		GetCount();
	}
	public static void CloseMenu () {
		print ("Close the menu");
		menuClosed = true;
		menuControl.SetActive (false);
	}
	private void GetCount () {
		totalObjs = 0;
		foreach (Transform dartObj in dartHolder) {
			Transform objectstotal = dartObj.GetComponentInChildren<Transform> ();
			totalObjs += objectstotal.childCount;
		}
		dartLimitText.text = "Dart Limit:\n " + totalObjs + " of 20";
	}
	private void CheckNewUser() {
		if (PlayerPrefs.GetInt("hasPlayedDarts") == 1) {
			print("Played");
			tutorialActive = false;
			tutorialMenu.SetActive(false);
		} else {
			Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
			laserLineRenderer.SetPositions (initLaserPositions);
			print("Not Played");
			tutorialMenu.SetActive(true);
			PlayerPrefs.SetInt("hasPlayedDarts", 1);
		}
	}
    private void CenterCam()
    {
        while (!tutorialMenuOpened)
        {
            float speed = Time.deltaTime * 5f;

            Vector3 pos = mainCam.transform.position + mainCam.transform.forward * 1.0f;
            menu.transform.position = Vector3.SlerpUnclamped(menu.transform.position, pos, speed);

            Quaternion rot = Quaternion.LookRotation(menu.transform.position - mainCam.transform.position);
            menu.transform.rotation = Quaternion.Slerp(menu.transform.rotation, rot, speed);
        }
    }
	private bool GetGesture(MLHand hand, MLHandKeyPose type) {
        if (hand != null) {				
            if (hand.KeyPose == type) {
                if (hand.KeyPoseConfidence > 0.9f) {                       
                    return true;
                }
            }
        }
        return false;
    }
}
