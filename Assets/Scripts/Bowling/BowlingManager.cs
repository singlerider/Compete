﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;

public class BowlingManager : MonoBehaviour {

	public enum holdState {
		none,
		single,
		tenPin,
		ball
	}
	public enum menuState {
		none,
		home,
		modifiers,
		changeBall
	}

	public enum HandPoses { OpenHandBack, Fist, NoPose };
	public HandPoses pose = HandPoses.NoPose;
	public Vector3[] pos;

	private MLHandKeyPose[] _gestures;

	public static holdState holding = holdState.single;
	public static menuState currentMenuState;

	// ML-Related objects.  "controller" manages input from the Control and "persistentBehavior" is to manage objects staying in place between session (not yet implemented)
	private MLInputController controller;
	public MLPersistentBehavior persistentBehavior;

	// Declare GameObjects.  Public GameObjects are set in Unity Editor.  
	public GameObject mainCam, orientationCube, control, tenPinOrientation, ballPrefab, menu, ballMenu, modifierMenu, tutorialMenu, controlCube, deleteLoader, menuCanvas, handCenter;
	public Text pinLimitText;
	public static GameObject menuControl;
	private GameObject bowlingBall;

	public Material transparent, activeMat;

	public Material[] ballMats, meshMats;

	public Transform singlePrefab, tenPinPrefab, pinHolder, singleNoGravityPrefab, tenPinNoGravityPrefab, meshHolder, planeHolder;

	public LineRenderer laserLineRenderer;

	public MeshRenderer mesh;

	private Vector3 endPosition, forcePerSecond;

	List<Vector3> Deltas = new List<Vector3> ();

	private float timeHold = 3.0f, totalObjs = 0, objLimit = 50, timeHomePress = 0.5f, timeOfFirstHomePress;

	private Controller checkController;

	public static float growSpeed = 5f;


	//public static string ballColor;

	public Image loadingImage;

	private bool setHand = false, placed = false, holdingBall = false, menuOpened = false, ballMenuOpened = false, holdingBallMenu = true, noGravity = false, tutorialActive = true, tutorialBumperPressed, tutorialHomePressed, tutorialMenuOpened = false, settingsOpened = false, occlusionActive = true, firstHomePressed = false;
	private static bool menuClosed = false;


	// Use this for initialization
	void Start () {

		// If the user is new, open the tutorial menu
		CheckNewUser ();
		// Start input from Control and Headpose
		MLInput.Start();

		// Get input from the Control, accessible via controller
		controller = MLInput.GetController (MLInput.Hand.Left);
		// When the Control's button(s) are pressed, run OnButtonDown
		MLInput.OnControllerButtonDown += OnButtonDown;

		// Initialize both line points at Vector3.Zero
		Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
		laserLineRenderer.SetPositions (initLaserPositions);

		// Access the object menu
		menuControl = GameObject.Find ("ObjectMenu");

		checkController = control.GetComponentInChildren<Controller> ();

		// Create the bowling ball at (100,100,100) so it cannot be seen by the user but can still be accessed
		bowlingBall = Instantiate (ballPrefab, new Vector3 (100, 100, 100), tenPinOrientation.transform.rotation);

		MLHands.Start();
		_gestures = new MLHandKeyPose[2];
		_gestures[0] = MLHandKeyPose.OpenHandBack;
		_gestures[1] = MLHandKeyPose.Fist;
		MLHands.KeyPoseManager.EnableKeyPoses(_gestures, true, false);
		pos = new Vector3[1];

    }
	private void OnDestroy () {
		MLInput.Stop ();
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

		// Always keep the control GameObject at the Control's position
		control.transform.position = controller.Position;
		control.transform.rotation = controller.Orientation;

		// If the user is not reading the tutorial menu, activate the line from the Control and prepare for the user to place objects

		if (tutorialActive == false) {
			SetLine ();
			PlaceObject ();
		} else {
			// TODO: While the tutorial menu is active, either disable the line completely or move it too far from the user to be visible
			
			// If the user presses anything while the tutorial is active, hide the tutorial and active the pointer
			if ((controller.Touch1Active || controller.TriggerValue >= 0.2f || tutorialBumperPressed == true || tutorialHomePressed == true) && tutorialMenuOpened == false) {
				laserLineRenderer.material = activeMat;
				CheckNewUser ();
			}
		}
		// If the user is touching the touchpad at all, show the object menu
		if (controller.Touch1Active) {
			if (menuClosed == false) {
				// If the menu is not yet open, open it
				menuControl.SetActive (true);
			}
			if (setHand == false) {
				setHand = true;
				Vector3[] zero = new Vector3[2] { Vector3.zero, Vector3.zero };
				laserLineRenderer.SetPositions (zero);
				menuControl.transform.position = controller.Position;
				menuControl.transform.rotation = mainCam.transform.rotation;
			}
		}
		if (controller.Touch1Active == false) {
			menuClosed = false;
			setHand = false;
			menuControl.SetActive (false);
		}


		// yee yee

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

		// Set the origin of the line to the controller's position.  Occurs every frame
		laserLineRenderer.SetPosition (0, controller.Position);

		if (Physics.Raycast (controller.Position, heading, out rayHit, 10.0f)) {
			// If the ray hits an object, set the line's end position to the distance between the controller and that point
			endPosition = controller.Position + (control.transform.forward * rayHit.distance);
			laserLineRenderer.SetPosition (1, endPosition);

			if (settingsOpened && controller.TriggerValue <= 0.2f)  {
				settingsOpened = false;
			}

			if (rayHit.transform.gameObject.name == "Home" && controller.TriggerValue >= 0.9f) {
				SceneManager.LoadScene ("Main", LoadSceneMode.Single);
                SceneManager.UnloadSceneAsync("Bowling");
            } else if (rayHit.transform.gameObject.name == "ChangeBall" && controller.TriggerValue >= 0.9f) {
				ballMenu.transform.position = mainCam.transform.position + (mainCam.transform.forward * 1.5f);
				ballMenu.transform.rotation = mainCam.transform.rotation;
				ballMenu.SetActive (true);
				menuClosed = true;
				menu.SetActive (false);
				ballMenuOpened = true;
				holdingBallMenu = true;
			} else if (rayHit.transform.gameObject.name == "Modifiers" && controller.TriggerValue >= 0.9f) {
				modifierMenu.SetActive (true);
				menu.SetActive (false);
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
				PlayerPrefs.SetInt ("hasPlayedBowling", 0);
				CheckNewUser ();
			} else if (rayHit.transform.gameObject.name == "NoGravity" && controller.TriggerValue >= 0.9f) {
                if (!settingsOpened)
                {
                    if (noGravity)
                    {
                        noGravity = false;
                    }
                    else
                    {
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
			if (!holdingBallMenu) {
				BowlingColorLoader.GetBallColor (rayHit, controller, ballMenu, ballMenuOpened, holdingBallMenu, bowlingBall, ballMats);
			} else if (holdingBallMenu && controller.TriggerValue <= 0.2f) {
				holdingBallMenu = false;
			}

		} else {
			// If no object is hit, make the length of the line 3 meters out from the controller
			endPosition = controller.Position + (control.transform.forward * 3.0f);
			laserLineRenderer.SetPosition (1, endPosition);
		}
	}
	private void PlaceObject () {
		if (holding == holdState.ball) {
			laserLineRenderer.material = transparent;
			if (controller.TriggerValue >= 0.9f) {
				if (holdingBall) {
					HoldingBall ();
				} else {
					holdingBall = true;
					BowlingColorLoader.LoadBallColor (bowlingBall, ballMats);
				}
			}
		} else if (holding == holdState.single) {
			laserLineRenderer.material = activeMat;
		} else if (holding == holdState.tenPin) {
			laserLineRenderer.material = activeMat;
		} else if (holding == holdState.none && tutorialMenuOpened == false) {
			laserLineRenderer.material = activeMat;
		}
		if (controller.TriggerValue >= 0.9f) {
			if (placed == false) {
				SpawnObject ();
				placed = true;
			}
		} else if (controller.TriggerValue <= 0.2f) {
			if (holdingBall == true) {
				Deltas.Clear ();
				holdingBall = false;
				var rigidbody = bowlingBall.GetComponent<Rigidbody> ();
				// Enable the rigidbody on the ball, then apply current forces to the ball
				rigidbody.useGravity = true;
				rigidbody.velocity = Vector3.zero;
				rigidbody.AddForce (forcePerSecond);
				forcePerSecond = Vector3.zero;
			} else {
				placed = false;
			}
		}
	}
	private void HoldingBall () {
		var rigidbody = bowlingBall.GetComponent<Rigidbody> ();
		rigidbody.velocity = Vector3.zero;

		//controlCube.SetActive (false);
		bowlingBall.transform.rotation = Quaternion.identity;
		var oldPosition = bowlingBall.transform.position;
		var newPosition = controller.Position;

		var delta = newPosition - oldPosition;
		if (Deltas.Count == 10) {
			Deltas.RemoveAt (0);
		}
		Deltas.Add (delta);
		Vector3 toAverage = Vector3.zero;
		foreach (var toAdd in Deltas) {
			toAverage += toAdd;
		}
		toAverage /= Deltas.Count;
		var forcePerSecondAvg = toAverage * 650;
		forcePerSecond = forcePerSecondAvg;
		bowlingBall.transform.position = controller.Position;
	}

	public static void CloseMenu () {
		menuClosed = true;
		menuControl.SetActive (false);
	}

	private void ClearAllObjects () {
		foreach (Transform child in pinHolder.transform) {
			GameObject.Destroy (child.gameObject);
		}

		totalObjs = 0;
		holding = holdState.none;
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
			// When the user presses the Home button and the menu is not opened, then open the menu
			laserLineRenderer.material = activeMat;
			menu.SetActive (true);

			modifierMenu.SetActive (false);
			menuOpened = true;
		} else if (button == MLInputControllerButton.HomeTap && menuOpened == true) {
			// If the user presses the Home button and the menu is opened, then close the menu
			menu.SetActive (false);
			menuOpened = false;
		}

		if (button == MLInputControllerButton.HomeTap && firstHomePressed) {
			if (Time.time - timeOfFirstHomePress < timeHomePress) {
				MLInput.Stop();
				MLHands.Stop();
				Application.Quit();
				timeOfFirstHomePress = 0f;
			}
			firstHomePressed = false;
		} else if (button == MLInputControllerButton.HomeTap && !firstHomePressed) {
			firstHomePressed = true;
			timeOfFirstHomePress = Time.time;
		}
	}
	private void SpawnObject () {
		// Set a limit as to how many objects can be spawned so framerate will not suffer
		if (totalObjs < objLimit) {
			// Check to see if the user has enabled the noGravity modifier
			if (!noGravity) {
				if (holding == holdState.single) {
					Instantiate (singlePrefab, endPosition, orientationCube.transform.rotation, pinHolder);
				} else if (holding == holdState.tenPin) {
					Instantiate (tenPinPrefab, endPosition, tenPinOrientation.transform.rotation, pinHolder);
				} else if (holding == holdState.ball) {
					Rigidbody ballRB = bowlingBall.GetComponent<Rigidbody> ();
					ballRB.useGravity = false;
				}
			} else if (noGravity) {
				if (holding == holdState.single) {
					Instantiate (singleNoGravityPrefab, endPosition, orientationCube.transform.rotation, pinHolder);
				} else if (holding == holdState.tenPin) {
					Instantiate (tenPinNoGravityPrefab, endPosition, tenPinOrientation.transform.rotation, pinHolder);
				} else if (holding == holdState.ball) {
					Rigidbody ballRB = bowlingBall.GetComponent<Rigidbody> ();
					ballRB.useGravity = false;
				}
			}
		} else {
			// Don't spawn any objects if there are more objects than the objLimit
			print ("Too many objects");
		}
		// Get a count of how many objects there are to ensure that there are not too many objects at once
		GetCount ();
		// TODO: Finish adding persistient behaviour to bowling and eventually darts
		//persistentBehavior.UpdateBinding();
	}
	private void GetCount () {
		totalObjs = 0;
		foreach (Transform bowlObj in pinHolder) {
			Transform objectstotal = bowlObj.GetComponentInChildren<Transform> ();
			totalObjs += objectstotal.childCount;
		}
		pinLimitText.text = "Pin Limit:\n " + totalObjs + " of 50";
	}
	private void CheckNewUser () {
		if (PlayerPrefs.GetInt ("hasPlayedBowling") == 1) {
			print ("Played");
			tutorialActive = false;
			laserLineRenderer.material = activeMat;
			tutorialMenu.SetActive (false);
		} else {
			Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
			laserLineRenderer.SetPositions (initLaserPositions);
			print ("Not Played");
			tutorialMenu.SetActive (true);
			PlayerPrefs.SetInt ("hasPlayedBowling", 1);
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