/*
The MIT License (MIT)
	
	Copyright (c) 2015 Jeff Hohenstein
		
		Permission is hereby granted, free of charge, to any person obtaining a copy
		of this software and associated documentation files (the "Software"), to deal
		in the Software without restriction, including without limitation the rights
		to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
		copies of the Software, and to permit persons to whom the Software is
		furnished to do so, subject to the following conditions:
		
		The above copyright notice and this permission notice shall be included in
		all copies or substantial portions of the Software.
		
		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
		IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
		FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
		AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
		LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
		OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
		THE SOFTWARE.
*/

/// <summary>
/// Example of using an FSM on a Unity game object in a 2D game.
/// 
/// This object is a ball that is being maneuvered toward a hole
/// where it will disappear.
/// 
/// When the ball is "Free" the ball takes input and moves around according to Physics.
/// 
/// When the ball is "Captured", it has gotten close enough to the hole to fall in. Animations are
/// followed by the ball disappearing. 
/// 
/// When the ball successfully "Flushed" (reaches the center of the hole), it notifies observers 
/// to change the score, plays a sound, and eventually destroyes itself.
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Saltatory.FSM;

public class Thingy : StateMachine {

	private Rigidbody2D rb ;
	private bool acceptInput = true;
	private GameObject well = null;
	private Dictionary<string,AudioSource> audioSources;

	// Set of FSM states used in this object
	private State stateFree ;
	private State stateCaptured ;
	private State stateFlushed ;

	// Input modifiers for tilt-physics
	public float XMultiplier = 10.0f;
	public float YMultiplier = 10.0f;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody2D>();

		// Create some states
		stateFree = AddState ("Free");
		stateCaptured = AddState ("Captured");
		stateFlushed = AddState ("Flushed");

		StartState.AddTransition (stateFree).AddTransition (stateCaptured).AddTransition (stateFlushed);

		// Get a list of all audio sources on children
		audioSources = new Dictionary<string, AudioSource> ();
		foreach (AudioSource s in GetComponentsInChildren<AudioSource> ()) {
			audioSources.Add (s.gameObject.name, s);
		}

		Transition (stateFree);

		// Apply a random velocity to the ball
		rb.velocity = new Vector2 (Random.value * 3, Random.value * 3);
		rb.AddTorque(Random.Range(1,3));

	}
	
	// Update is called once per frame
	void Update () {
		switch (CurrentState.Name) {
			case "Free":
				HandleInput();
				break;
		case "Captured":
			Flush();
			break;
		case "Flushed":
			Flushed ();
			break;
		}
	}

	// Called on Update to handle inputs
	void HandleInput(){
		if (acceptInput) {
			rb.AddForce (new Vector2 (Input.acceleration.x * XMultiplier, Input.acceleration.y * YMultiplier));

			// Get the vector from the ball position to the mouse
			if(Input.GetMouseButtonUp(0)){
				Vector2 mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				rb.AddForce((mPos - rb.position).normalized * 50);
			}
		}
	}

	// Apply strong gravity toward center of hole
	void Flush(){
		rb.AddForce( (well.transform.position - rb.transform.position) * 10 ); 
	}

	void Flushed(){
		rb.position += ((Vector2)well.transform.position - rb.position).normalized * .01f;
	}

	// Change some parameters when the ball transitions between states
	// Overrides StateMachine::OnTransition(...)
	public override void OnTransition(State from, State to) {
		Debug.Log (string.Format ("OnTransition to {0}", to.Name));

		switch(to.Name){
		case "Free":

			// Start accepting Input in the Update loop
			acceptInput = true;
			break;

		case "Captured":

			// Turn off input and change the Physics properties so it slows down
			rb.drag = 1.5f;
			rb.angularDrag = 1.0f;
			acceptInput = false;
			GetComponent<Animation>().Play("Alert");
			break;

		case "Flushed":

			// Stop the ball in favor of the flush animation. Play a sound. 
			// Update observers then destroy the object.
			rb.velocity = new Vector2(0,0);
			// Turn off the collider and therefore the physics
			GetComponent<CircleCollider2D>().enabled = false;
			GetComponent<Animation>().Play("BallFlush");

			foreach(KeyValuePair<string,AudioSource> s in audioSources){
				if(s.Key == "Disappear"){
					s.Value.PlayOneShot(s.Value.clip,1);
				}
			}

			Destroy(gameObject,5);
			break;

		}

	}

	void OnTriggerEnter2D(Collider2D c){
		Debug.Log ("Hit");

		// If I collided with the hole, start flushing
		if (c.CompareTag ("GravityWell")) {

			well = c.gameObject;
			Transition (stateCaptured);

		} else if (c.CompareTag ("Hole")) {

			Transition (stateFlushed);

		}

	}

}
 