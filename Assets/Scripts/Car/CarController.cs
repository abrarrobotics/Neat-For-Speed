﻿using UnityEngine;

namespace airace {

    /// <summary>
    /// Class controlling the car, acceleration, braking and turning.
    /// The player controller or ANN can call the control function with an intensity optional parameter:
    /// The intensity is 0.75 by default for a simple integration and can be between 0 and 1 for more complexity.
    /// Available controls are Drive(), Turn(), Brake()
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour {

        // car behavior related variables
        private float steeringRate = 100f;
        private float brakingRate = 20f;
        private float acceleration = 30f;
        private float maxForwardSpeed = 30f;
        private float maxReverseSpeed = -10f;
        private float frictionBrake = 10f;
        private float aboutZero = 0.01f;
        private float speed = 0f;
        private float Speed {
            set {
                speed = Mathf.Clamp(value, maxReverseSpeed, maxForwardSpeed);
                if ((Mathf.Abs(speed) - aboutZero) < aboutZero)
                    speed = 0f;
            }
			get { return speed; }
        }

        // axis force related variable
        private float forceChangeRate = 0.05f; // I made a script to check rate of value change of a keyboard axis and it was ~0.0495
        private float driveForce = 0f;
        private float turnForce = 0f;
        // public acces to the current values of the axis in case it is need for the AI
        public float DriveForce { get { return driveForce; } }
        public float TurnForce { get { return turnForce; } }


		/// <summary>
		/// Will return speed value from -1 to 1.
		/// </summary>
		public float NormalizedSpeed {
			get {
				if(speed >= 0)
					return speed/maxForwardSpeed;
				else
                    return -speed / maxReverseSpeed;
			}
		}

		// bool use to initiate the rest car process
        private bool reset = false; // reset will block controls
		
		// reference to the car Rigidbody
        private Rigidbody car;

        private void Start() {
            car = GetComponent<Rigidbody>();
        }

        // Updates the car movement if speed not at 0 and reset the car if necessary
        private void Update() {
            if (reset) {
                Brake(1f);

                if (Speed == 0f)
                    Reset();
            }

            if (Speed != 0f) {
                FrictionEffect();
                MoveCar();
            }
        }

		// Default slow down effect running each frame
        private void FrictionEffect() {
			if(Speed > 0)
            	Speed -= frictionBrake * Time.deltaTime;
			else
				Speed += frictionBrake * Time.deltaTime;
        }

		// Updates the car position each frame depending on speed.
        private void MoveCar() {
            car.MovePosition(transform.position + transform.forward * Speed * Time.deltaTime);
        }

		// called when there is a collision to reset the car
        private void OnTriggerEnter(Collider other) {
            reset = true;
            if(other.gameObject.tag == "wall"){
				reset = true;
				// Destroy(gameObject);
			} else if(other.gameObject.tag == "car"){
				// reset = true;
				// Destroy(gameObject);
			}
        }

		// resets the car to the start state
        private void Reset() {
            transform.position = new Vector3(0f, 0.5f, 0f);
            transform.rotation = Quaternion.identity;
            car.velocity = Vector3.zero;
            reset = false;
        }

        /// <summary>
		/// Modifies an axis force by reference. The idea being that an ANN should not be able
        /// to jump from one side of the whell to the other to keep a human behavior.
        /// This is done naturally with a keyboard or joystick, so this function enforces
        /// this gradual change of value. I checked with a script to make sure this rate
        /// is similar to the one with a keyboard in unity.
		/// </summary>
        private float GetForce(ref float force, float targetForce) {

            // if the target value is above the current value
            if(targetForce > force && (targetForce - force) < forceChangeRate)
                force = targetForce;

            else if(targetForce > force)
                force += forceChangeRate;

            // if the target value is under the current value
            else if(targetForce < force && (force - targetForce) < forceChangeRate)
                force = targetForce;
            
            else if(targetForce < force)
                force -= forceChangeRate;
            
            force = Mathf.Clamp(force, -1f, 1f);

            return force;
        }


		// Public Control Intention Methods

		// Is called from the control methods to update the speed value
		public void Drive(float targetForce) {

            // gets the actual force gradually changed toward the targetForce
            float force = GetForce(ref driveForce, targetForce);

            if (!reset){
                if((Speed < 0f && force >= 0f) || (Speed > 0f && force < 0f))
                    Brake(force);
                else
                    Speed += Mathf.Clamp(force, -1f, 1f) * acceleration * Time.deltaTime;
            }
        }

		// Is called from the control methods to turn the car
		public void Turn(float targetForce) {

            // gets the actual force gradually changed toward the targetForce
            float force = GetForce(ref turnForce, targetForce);

            if (!reset) {
                float relativeSpeed = Speed >= 0 ? Speed / maxForwardSpeed : Speed / maxReverseSpeed;
                float turnValue = Mathf.Clamp(force, -1f, 1f) * steeringRate * relativeSpeed * Time.deltaTime;
                transform.Rotate(0f, turnValue, 0f);
            }
        }

		/// <summary>
		/// Get the speed closer to 0 by the force and brake rate.
		/// </summary>
        public void Brake(float force = 0.75f) {
            float brakeValue = brakingRate * Mathf.Clamp01(Mathf.Abs(force)) * Time.deltaTime;
            
            if(Mathf.Abs(Speed) < aboutZero)
                Speed = 0f;
            else if(speed > 0)
                Speed -= brakeValue;
            else
                Speed += brakeValue;
        }

    }
}
