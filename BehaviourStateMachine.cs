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

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Saltatory.FSM
{
	/// <summary>
	/// Exception thrown when the FSM cannot complete the requested transition.
	/// </summary>
	public class TransitionException : Exception
	{
		public TransitionException (string message):	base (message)
		{
		}
	}

	/// <summary>
	/// Arguments passed from sender to received when a Transition event is sent.
	/// 
	/// Contains a reference to the state machine, the from and requested to states.
	/// </summary>
	public class TransitionArgs : EventArgs {

		public BehaviourState From ;
		public BehaviourState To ;
		public BehaviourStateMachine StateMachine;

		public TransitionArgs(BehaviourState From, BehaviourState To, BehaviourStateMachine StateMachine){
			this.From = From;
			this.To = To;
			this.StateMachine = StateMachine;
		}
	}


	/// <summary>
	/// Definition for an event handler to receive notifications when a state transition occurs.
	/// </summary>
	public delegate void TransitionEventHandler(object sender, TransitionArgs args);

	/// <summary>
	/// A state in the state machine.
	/// </summary>
	public class BehaviourState
	{
		private BehaviourStateMachine machine ;
		private HashSet<BehaviourTransition> transitions;

		/// <summary>
		/// Get the State Machine. Only a getter because only the machine should
		/// create a state (factory).
		/// </summary>
		/// <value>The machine.</value>
		public BehaviourStateMachine Machine { get{ return machine; } }

		/// <summary>
		/// Name of the state. Freely settable/gettable. Only used for human readability.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }

		/// <summary>
		/// Transitions from this state to Transitions leading to other states.
		/// </summary>
		/// <value>The transitions.</value>
		public  HashSet<BehaviourTransition> Transitions { get { return transitions; } }

		public BehaviourState (string name,BehaviourStateMachine machine)
		{
			this.Name = name;
			this.machine = machine;
			this.transitions = new HashSet<BehaviourTransition> ();
		}

		/// <summary>
		/// Factory method to add a transition from the current state to the given state.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="state">State.</param>
		public BehaviourState AddTransition (BehaviourState state)
		{	
			machine.AddTransition (this, state);
			return state;
		}

		/// <summary>
		/// No package private. Called by Machine.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="transition">Transition.</param>
		public BehaviourTransition AddTransition(BehaviourTransition transition){
			if (transition.State.Machine != machine) {
				throw new TransitionException ("The to state in this transition is from another state machine than this one.");
			}

			transitions.Add (transition);
			return transition;
		}

		/// <summary>
		/// No package private. Called by machine.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="transition">Transition.</param>
		public BehaviourState RemoveTransition (BehaviourTransition transition)
		{
			if (transitions.Contains(transition)) {
				transitions.Remove (transition);
			}

			return this;
		}

	}
		
	/// <summary>
	/// Transition. Only holds reference to To state, not from state.
	/// </summary>
	public class BehaviourTransition
	{
		private BehaviourState state ;

		/// <summary>
		/// For readability. Later, could be used if Transitions have more code attached.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }
			
		/// <summary>
		/// State to which this transitions points.
		/// </summary>
		/// <value>The states.</value>
		public BehaviourState State { get { return state; } }
			
		/// <summary>
		/// No package private. Called by machine.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="state">State.</param>
		public BehaviourTransition (string name, BehaviourState state)
		{
			this.Name = name;
			this.state = state;
		}

		/// <summary>
		/// Convenience constructor to give a name.
		/// </summary>
		/// <param name="state">State.</param>
		public BehaviourTransition(BehaviourState state){
			this.Name = string.Format ("To {0}", state.Name);
			this.state = state;
		}
			
	}

	/// <summary>
	/// A state machine for Unity. Derive stateful classes from this one.
	/// 
	/// All graphs start with a state called "Start". All other states should be 
	/// attached transitively to the start state. States are stored in Sets, not
	/// Dictionary s so best practice is to hold references to created states rather
	/// than referring to them as strings.
	/// </summary>
	public abstract class BehaviourStateMachine : MonoBehaviour
	{

		private BehaviourState start ;
		private BehaviourState currentState ;
		private HashSet<BehaviourState> states;
		private HashSet<BehaviourTransition> transitions;

		public BehaviourState StartState { get { return start; } }
		public BehaviourState CurrentState { get { return currentState; } }
		public HashSet<BehaviourState> States { get { return states; } }
		public HashSet<BehaviourTransition> Transitions { get { return transitions; } }

		public BehaviourStateMachine ()
		{
			states = new HashSet<BehaviourState> ();
			transitions = new HashSet<BehaviourTransition> ();

			start = AddState ("Start");
			currentState = start;
		}

		/// <summary>
		/// Event handler definition for transitions. Implement this in non-inheritors
		/// interested in receiving notifications of transitions.
		/// </summary>
		public event TransitionEventHandler Transitioned ;

		/// <summary>
		/// Add a state with no transitions in or out.
		/// </summary>
		/// <returns>The state.</returns>
		/// <param name="name">Name.</param>
		public BehaviourState AddState (string name)
		{
			BehaviourState state = new BehaviourState (name,this);
			states.Add (state);
			return state;
		}

		/// <summary>
		/// Remove the state and all transitions from upstream states.
		/// </summary>
		/// <returns>The state.</returns>
		/// <param name="state">State.</param>
		public BehaviourStateMachine RemoveState (BehaviourState state)
		{
			if (state == StartState) {
				throw new ArgumentException ("Cannot remove start state");
			} else {

				states.Remove (state);

				foreach (BehaviourTransition t in state.Transitions) {
					state.RemoveTransition (t);
				}

				return this;
			}
		}

		/// <summary>
		/// Add a Transition from one state to another.
		/// 
		/// Used in constructing the graph.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		public BehaviourTransition AddTransition (BehaviourState from, BehaviourState to)
		{
			if (!States.Contains (from) || !States.Contains (to))
				throw new TransitionException ("One or both of the states does not exist");
			
			BehaviourTransition transition = new BehaviourTransition (to);
			from.AddTransition (transition);
			
			transitions.Add (transition);
			return transition;
		}

		/// <summary>
		/// Remove a transition.
		/// 
		/// Used in editing the graph.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="transition">Transition.</param>
		public BehaviourStateMachine RemoveTransition (BehaviourTransition transition)
		{
			// Find all states referencing this transition and remove the reference
			foreach (BehaviourState s in states) {
				s.RemoveTransition (transition);
			}

			return this;
		}

		/// <summary>
		/// Ask the machine to transition from the current state to the requested state.
		/// 
		/// Raises a TransitionException if the requested state is not reachable.
		/// </summary>
		/// <param name="state">State.</param>
		public void Transition (BehaviourState state)
		{
			Debug.Log (string.Format("Transitioning from {0} to {1}", CurrentState.Name, state.Name));
			if (CurrentState != state) {
				foreach (BehaviourTransition t in CurrentState.Transitions) {
					if (t.State == state) {
						currentState = state;
						// return OnTransition (currentState, state);
						OnTransition (currentState, state);

						// Throw the event
						if(Transitioned != null)
							Transitioned(this, new TransitionArgs(currentState,state,this));
						return;
					}
				}
				throw new TransitionException (string.Format ("State {0} is not accessible from the current state {1}", state.Name, CurrentState.Name));
			}

		}

		/// <summary>
		/// Raises the transition event. Override this method in inheritors to write code
		/// that occurs on Transition. 
		/// 
		/// For non-inheritors, use the event instead.
		/// </summary>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		public abstract void OnTransition (BehaviourState from, BehaviourState to) ;


	}

}
