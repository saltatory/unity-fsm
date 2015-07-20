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

		public State From ;
		public State To ;
		public StateMachine StateMachine;

		public TransitionArgs(State From, State To, StateMachine StateMachine){
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
	public class State
	{
		private StateMachine machine ;
		private HashSet<Transition> transitions;

		/// <summary>
		/// Get the State Machine. Only a getter because only the machine should
		/// create a state (factory).
		/// </summary>
		/// <value>The machine.</value>
		public StateMachine Machine { get{ return machine; } }

		/// <summary>
		/// Name of the state. Freely settable/gettable. Only used for human readability.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }

		/// <summary>
		/// Transitions from this state to Transitions leading to other states.
		/// </summary>
		/// <value>The transitions.</value>
		public  HashSet<Transition> Transitions { get { return transitions; } }

		public State (string name,StateMachine machine)
		{
			this.Name = name;
			this.machine = machine;
			this.transitions = new HashSet<Transition> ();
		}

		/// <summary>
		/// Factory method to add a transition from the current state to the given state.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="state">State.</param>
		public State AddTransition (State state)
		{	
			machine.AddTransition (this, state);
			return state;
		}

		/// <summary>
		/// No package private. Called by Machine.
		/// </summary>
		/// <returns>The transition.</returns>
		/// <param name="transition">Transition.</param>
		public Transition AddTransition(Transition transition){
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
		public State RemoveTransition (Transition transition)
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
	public class Transition
	{
		private State state ;

		/// <summary>
		/// For readability. Later, could be used if Transitions have more code attached.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }
			
		/// <summary>
		/// State to which this transitions points.
		/// </summary>
		/// <value>The states.</value>
		public State State { get { return state; } }
			
		/// <summary>
		/// No package private. Called by machine.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="state">State.</param>
		public Transition (string name, State state)
		{
			this.Name = name;
			this.state = state;
		}

		/// <summary>
		/// Convenience constructor to give a name.
		/// </summary>
		/// <param name="state">State.</param>
		public Transition(State state){
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
	public abstract class StateMachine : MonoBehaviour
	{

		private State start ;
		private State currentState ;
		private HashSet<State> states;
		private HashSet<Transition> transitions;

		public State StartState { get { return start; } }
		public State CurrentState { get { return currentState; } }
		public HashSet<State> States { get { return states; } }
		public HashSet<Transition> Transitions { get { return transitions; } }

		public StateMachine ()
		{
			states = new HashSet<State> ();
			transitions = new HashSet<Transition> ();

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
		public State AddState (string name)
		{
			State state = new State (name,this);
			states.Add (state);
			return state;
		}

		/// <summary>
		/// Remove the state and all transitions from upstream states.
		/// </summary>
		/// <returns>The state.</returns>
		/// <param name="state">State.</param>
		public StateMachine RemoveState (State state)
		{
			if (state == StartState) {
				throw new ArgumentException ("Cannot remove start state");
			} else {

				states.Remove (state);

				foreach (Transition t in state.Transitions) {
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
		public Transition AddTransition (State from, State to)
		{
			if (!States.Contains (from) || !States.Contains (to))
				throw new TransitionException ("One or both of the states does not exist");
			
			Transition transition = new Transition (to);
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
		public StateMachine RemoveTransition (Transition transition)
		{
			// Find all states referencing this transition and remove the reference
			foreach (State s in states) {
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
		public void Transition (State state)
		{
			Debug.Log (string.Format("Transitioning from {0} to {1}", CurrentState.Name, state.Name));
			if (CurrentState != state) {
				foreach (Transition t in CurrentState.Transitions) {
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
		public abstract void OnTransition (State from, State to) ;


	}

}
