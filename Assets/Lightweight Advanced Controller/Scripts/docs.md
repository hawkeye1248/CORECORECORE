# Lightweight Advanced FPS Controller Docs

This is the basic documentation for the package. The main script is also fully commented to help you better understand the intricacies of how the package functions if need be. The purpose of this documentation isn't to be an exhaustive list of every method and how the movement itself functions, but rather to explain the architectural structure of the package and how it can be used and expanded in your project.

## Behaviour

The script is designed primarily for use with mouse and keyboard input, however any input could be used theoretically.

Movement is handled in discrete states, such as *walking*, *sprinting*, or *airborne*, which operate independently and don't clash with eachother.

The movement system revolves around an optional stamina system. Stamina is only required to sprint, however all other movement options have minimum speed options which can only be met by sprinting. Stamina is spent by certain actions, such as jumping and sprinting, and can be regained over time or by performming certain moves such as mantling.

## PlayerMovement.cs

This is the main script for player movement. It contains a plethora of values that can be manipulated to give the desired movement for your game, not every individual value and movement function will be explained here, but they all have tooltips and summary comments within Unity which explain their functionality.

### MoveState

The movement behaviour is driven by the ``MoveState`` enum, which contains all possible states the player can be in. By default, these are

```C#
* walk
* crouch
* air
* sprint
* slide
* wallrun
* none //unused by the movement script, just a placeholder/default value.
```

The current MoveState is stored in the private field currentState.

Every FixedUpdate, the script runs the ``Think`` method. This method is responsible for checking the current state of the controller, and running the corresponding method (prefixed with *Manage* for clarity, e.g ``ManageWalk`` for ``Walk``). Also during FixedUpdate, before managing the player's current state, the stamina system is managed.

### Adding new MoveStates

This will guide you through the process of creating a new movement state. For this example, we will create a ``swimming`` state.
First, add the state to the MoveState enum.

We are going to want to create a ``swimSpeed`` value, exposed to the inspector, to control how fast the player swims. Add this at the top in the ``Values`` region: ``[SerializeField] private float swimSpeed``

Next, create a ``ManageSwimming()`` method. For this example swimming will move the player in the direction of their camera (including moving upwards) according to their input. This method would look like this:

```C#
private void ManageSwimming () {
    Vector3 moveDirection = camera.transform.forward * new Vector3(inputAxes.x, 0, inputAxes.y);
    velocity = moveDirection * swimSpeed;
}
```

Add the method it to the switch statement in the think method. The result should like like this:

```C#
private void Think()
{
    switch (currentState)
    {
        case MoveState.walk:
            ManageWalk();
            break;
        case MoveState.air:
            ManageAir();
            break;
        case MoveState.crouch:
            ManageCrouch();
            break;
        case MoveState.sprint:
            ManageSprint();
            break;
        case MoveState.slide:
            ManageSlide();
            break;
        case MoveState.wallrun:
            ManageWallrun();
            break;
        case MoveState.swimming:
            ManageSwimming();
            break;
    }
}
```

Finally, we need to be able to put the player into the swimming state, and take them out. To do so, we will use an [``OverlapBox``](https://docs.unity3d.com/ScriptReference/Physics.OverlapBox.html) to check if the player is within a trigger labelled Water. Create a method for checking if the player is submerged:

```C#
private bool IsPlayerSubmerged () {
    //Check box from the player's center extending in each direction by the player's height.
    //4 is used as the LayerMask as this is Unity's built-in Water layer.
    //Return true if at least one collider is found, i.e the player is in water.
    return Physics.OverlapBox(transform.position, Vector3.one * cc.height, transform.rotation, 4).Length > 0;
}
```

We can then use this to take the player out of their swimming state by adding these lines to the ``ManageSwimming`` method:

```C#
if (!IsPlayerSubmerged()) {
    //Defaults to airborne as this can transition to whatever state is most appropriate
    SetMovementState(MoveState.air);
}
```

Conversely, to put the player into their swimming state we have to check if they are submerged and then set the state. Where you do this is up to you. I insterted it into the ``CheckGround`` method since this is called every ``FixedUpdate``, and the swimming state should override any other movement state. The code to set the state looks like this:

```C#
if (IsPlayerSubmerged()) {
    SetMovementState(MoveState.swimming);
}
```

While this might seem like a lot of work, this segmentation of the different behaviours is crucial as projects become more complicated. It completely nullifies the possibility of code from different behaviours conflicting, since each state is so discrete.

## Stamina

Stamina can be enabled/disabled by the ``useStamina`` flag. When disabled, stamina is constantly set to 1 before any movement is handled, ensuring the player always has enough stamina to perform any action.

Stamina ranges from 0-1. It starts at 1 and depletes at a fixed rate (``staminaDrainRate``) while sprinting. Once it is empty, it will remain empty for a period (``staminaWaitToRefill``) before then regenerating (based on ``staminaRegenRate``).

Stamina can be granted using the ``GrantStamina`` method.

## Input

Button inputs are managed by a set of flags.

e.g, to tell the controller to jump from your input script, you would write something like:

```C#
if (Input.GetButtonDown("Jump")) {
    //Assuming movement is a reference to the active PlayerMovement component.
    movement.FlagJumpInput();
}
```

The boolean flags for jumping, sliding, and sprinting are all reset after FixedUpdate is called.

For the movement axes, a similar approach is used however the values *aren't reset* after FixedUpdate.
The axes are stored as a Vector2.
e.g:
``
movement.SetInputAxes(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
``

## Effect Events

Certain actions within the script invoke certain visuals, such as camera FOV changes or UI updates. Instead of tying these to a specific script, a series of events are used. You can have your own game's systems subscribe to these, or use the scripts provided as part of the demo package.

To use the events, you first need an appropriate method. For this example, I'll use the ``fxAdjustFOV`` event. This is used to multiply the current FOV value by ``multiplier``, over ``time``.

I have a coroutine which is used to change the camera FOV to a given value over time called ``SetFovCoroutine`` which takes in a float for the final FOV and another float for the time taken for the transition to complete. When the adjust FOV event is called, I want to calculate the target FOV and start this coroutine. The method to do so looks like this:

```C#
public void MultiplyFov(float _multiplier, float _time)
{
    StartCoroutine(SetFovCoroutine(defaultFov * _multiplier, _time));
}
```

I then subscribe to ``fxAdjustFOV`` event on ``Start``.

```C#
[SerializeField] private PlayerMovement movement; //A reference to the PlayerMovement component.
...

private void Start () {
    movement.fxAdjustFOV += MultiplyFov;
}
```

Now when the controller wants to change the FOV this method is invoked, and you have full control over how it operates giving you a lot of customisability without needing to modify the movement script itself.

For a better look at how this system works, take a look at the demo CameraEffects.cs and UIManager.cs scripts.

## Audio

Audio is handled by the AudioManager.cs script. It is seperated into two primary components: First, state-based sound rules, and velocity-based wind sound.

### State-based Sound

When the player controller changes state, the ``HandleStateChange`` method is called, informing the script of the new state. The script contains a list of rules provided in the inspector, with some already setup in the demo that just need audio files supplied (walking, sprinting, and landing on the ground). These rules are used when the state changes to decide what audio file to play based on the current and previous movement state, containing valid 'impulse' sounds which are played once, or looping audio clips. There is also support for preventing looping sounds being played when the player is stationary while being in a specific state, ideal for footstep sounds.

As an example, one of the included sound rules is setup to play an impulse sound when the state changes from airborne to any other state. This is used as a 'landing' sound. Furthermore, there is a rule for a looping walk sound when any state transitions to walking, as well as a slightly faster footstep cycle for sprinting. With these simple rulesets, you can create most combinations of sounds.

For ease-of-use, all the audio clip fields have been populated with appropriately-named, but empty, clips. Replace these with your own properly licensed sound effects.

### Velocity-based Wind Sound

This system modulates the volume of a looping audio source according to the player's velocity on a curve. While in theory this could be used for anything, in practise it is best used for wind noise. Once the player reaches the given speed threshold, it evaluates the volume along the curve, up to the maximum volume, with twice the speed threshold being considered the maximum speed. It's simple, just one line of code, but adds a lot to the feeling of speed. To enable the wind sound, assign your chosen wind sound to the WindSource GameObject, a child of the demo player.
