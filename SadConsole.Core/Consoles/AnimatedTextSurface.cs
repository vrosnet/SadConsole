﻿namespace SadConsole.Consoles
{
    using Consoles;
    using Microsoft.Xna.Framework;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Animates a list of frames.
    /// </summary>
    public class AnimatedTextSurface: TextSurface
    {
        /// <summary>
        /// Raised when the <see cref="AnimationState"/> changes.
        /// </summary>
        public event System.EventHandler<AnimationStateChangedEventArgs> AnimationStateChanged;

        #region Variables
        /// <summary>
        /// Time counter for the naimation
        /// </summary>
        protected double _addedTime;
        
        /// <summary>
        /// The current frame index being animated.
        /// </summary>
        protected int _currentFrameIndex;

        /// <summary>
        /// The length of the animation
        /// </summary>
        protected float _animatedTime;

        /// <summary>
        /// How much time per animated frame should be used.
        /// </summary>
        protected float _timePerFrame;

        /// <summary>
        /// Indicates the animation is currently animating.
        /// </summary>
        protected bool _isPlaying;

        /// <summary>
        /// The font to use with all frames.
        /// </summary>
        protected Font _font = Engine.DefaultFont;

        /// <summary>
        /// All frames of the animation
        /// </summary>
        public List<TextSurfaceBasic> Frames = new List<TextSurfaceBasic>();

        /// <summary>
        /// The state of the animation.
        /// </summary>
        protected AnimationState state;
        

        #endregion

        #region Properties
        /// <summary>
        /// Center of the animation used in positioning.
        /// </summary>
        public Point Center { get; set; }

        /// <summary>
        /// Indicates whether or not this animation will repeat once it has finished animating.
        /// </summary>
        public bool Repeat { get; set; }

        /// <summary>
        /// When true, the <see cref="Update"/> method will advance the frames.
        /// </summary>
        public bool IsPlaying { get { return _isPlaying; } }

        /// <summary>
        /// The length of the animation.
        /// </summary>
        public float AnimationDuration
        {
            get { return _animatedTime; }
            set { _animatedTime = value; CalculateFrameDuration(); }
        }

        /// <summary>
        /// Gets or sets the current frame index to animate.
        /// </summary>
        public int CurrentFrameIndex
        {
            get { return _currentFrameIndex; }
            set
            {
                if (value < 0 || value >= Frames.Count)
                    _currentFrameIndex = 0;
                else
                    _currentFrameIndex = value;

                UpdateFrameReferences();
            }
        }
        
        /// <summary>
        /// Indicates the animation is empty.
        /// </summary>
        public bool IsEmpty { get { return Frames.Count == 0; } }

        /// <summary>
        /// Gets the name of this animation.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets the currently frame being animated.
        /// </summary>
        public TextSurfaceBasic CurrentFrame
        {
            get { return Frames[_currentFrameIndex]; }
        }

        /// <summary>
        /// Gets the current animation state.
        /// </summary>
        public AnimationState State
        {
            get { return state; }
            internal set
            {
                var oldState = state;

                if (value != state)
                {
                    state = value;
                    AnimationStateChanged?.Invoke(this, new AnimationStateChangedEventArgs(oldState, state));
                }
            }
        }
        
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new animation with the specified name, width, and height.
        /// </summary>
        /// <param name="name">The name of the animation.</param>
        /// <param name="width">The width of each frame this animation will have.</param>
        /// <param name="height">The height of each frame this animation will have.</param>
        /// <param name="font">The font used with this animation.</param>
        public AnimatedTextSurface(string name, int width, int height, Font font): base(width, height, font)
        {
            Name = name;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Forces the area of this text surface to always be the full width and height.
        /// </summary>
        protected override void ResetArea()
        {
            // Sub views are not allowed on animated text surfaces. Enforce full view.
            area = new Rectangle(0, 0, width, height);

            base.ResetArea();

            if (Frames.Count > 0)
                UpdateFrameReferences();
        }

        /// <summary>
        /// Updates the base <see cref="TextSurface"/> render references to the current frame.
        /// </summary>
        protected void UpdateFrameReferences()
        {
            var frame = Frames[_currentFrameIndex];
            RenderCells = frame.Cells;
            DefaultBackground = frame.DefaultBackground;
            DefaultForeground = frame.DefaultForeground;
        }

        /// <summary>
        /// Creates a new frame with the same dimensions as this entity and adds it to the Frames collection of the entity.
        /// </summary>
        /// <returns>The created frame.</returns>
        public TextSurfaceBasic CreateFrame()
        {
            if (Frames == null)
                Frames = new List<TextSurfaceBasic>();

            var frame = new TextSurfaceBasic(Width, Height);
            Frames.Add(frame);
            UpdateFrameReferences();
            return frame;
        }

        /// <summary>
        /// Calculates the time needed per frame for rendering.
        /// </summary>
        private void CalculateFrameDuration()
        {
            if (IsEmpty || _animatedTime == 0f)
                _timePerFrame = 0f;
            else
                _timePerFrame = _animatedTime / Frames.Count;
        }
        
        /// <summary>
        /// Stops animating.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            State = AnimationState.Stopped;
        }

        /// <summary>
        /// Starts animating the frames.
        /// </summary>
        public void Start()
        {
            CalculateFrameDuration();
            _isPlaying = true;
            State = AnimationState.Playing;
        }

        /// <summary>
        /// Restarts the animation from the first frame.
        /// </summary>
        public void Restart()
        {
            CalculateFrameDuration();
            _isPlaying = true;
            CurrentFrameIndex = 0;
            State = AnimationState.Restarted;
            State = AnimationState.Playing;
        }

        /// <summary>
        /// Updates the animation frames based on the time passed since the last call to this method.
        /// </summary>
        public void Update()
        {
            if (_isPlaying && _timePerFrame != 0f)
            {
                // TODO: Evaluate if we should change this to calculate current frame based on total time passed,
                // not calculate frame based on individual frame duration on screen.
                _addedTime += Engine.GameTimeElapsedUpdate;

                if (_addedTime > _timePerFrame)
                {
                    _addedTime = 0f;
                    _currentFrameIndex++;

                    if (_currentFrameIndex >= Frames.Count)
                    {
                        if (Repeat)
                        {
                            _currentFrameIndex = 0;
                            State = AnimationState.Restarted;
                            State = AnimationState.Playing;
                        }
                        else
                        {
                            _isPlaying = false;
                            _currentFrameIndex--;
                            State = AnimationState.Finished;
                        }
                    }

                    UpdateFrameReferences();
                }
            }
        }

        /// <summary>
        /// Returns the name of the animation.
        /// </summary>
        /// <returns>The name.</returns>
        public override string ToString()
        {
            return Name;
        }

        #endregion

        public void Save(string file)
        {
            Serialized.Save(this, file);
        }

        public static AnimatedTextSurface Load(string file)
        {
            return Serialized.Load(file);
        }


        /// <summary>
        /// Event args for when the animation state changes
        /// </summary>
        public class AnimationStateChangedEventArgs : System.EventArgs
        {
            /// <summary>
            /// The previous state.
            /// </summary>
            public readonly AnimationState PreviousState;

            /// <summary>
            /// The new state.
            /// </summary>
            public readonly AnimationState NewState;

            /// <summary>
            /// Creates a new instance of the event args.
            /// </summary>
            /// <param name="previousState">The previous state.</param>
            /// <param name="newState">The new state.</param>
            public AnimationStateChangedEventArgs(AnimationState previousState, AnimationState newState)
            {
                PreviousState = previousState;
                NewState = newState;
            }
        }

        /// <summary>
        /// Represents what state the animation is in.
        /// </summary>
        public enum AnimationState
        {
            /// <summary>
            /// The animation has never been played or was forcibly stopped.
            /// </summary>
            Stopped,

            /// <summary>
            /// The animation is currently playing.
            /// </summary>
            Playing,

            /// <summary>
            /// The animation was either manually restarted or repeated.
            /// </summary>
            Restarted,

            /// <summary>
            /// The animation was played and completed.
            /// </summary>
            Finished,

            /// <summary>
            /// The animation is now the current animation for an entity.
            /// </summary>
            Activated,

            /// <summary>
            /// The animation is no longer the current animation for an entity.
            /// </summary>
            Deactivated
        }


        /// <summary>
        /// Serialized instance of a <see cref="AnimatedTextSurface"/>.
        /// </summary>
        [DataContract]
        public class Serialized
        {
            [DataMember]
            public TextSurfaceBasic[] Frames;
            [DataMember]
            public int Width;
            [DataMember]
            public int Height;
            [DataMember]
            public float AnimationDuration;
            [DataMember]
            public string FontName;
            [DataMember]
            public Font.FontSizes FontSize;
            [DataMember]
            public string Name;
            [DataMember]
            public bool Repeat;
            [DataMember]
            public Point Center;

            public Serialized(AnimatedTextSurface surface)
            {
                Frames = surface.Frames.ToArray();
                Width = surface.width;
                Height = surface.height;
                AnimationDuration = surface.AnimationDuration;
                Name = surface.Name;
                FontName = surface.font.Name;
                FontSize = surface.font.SizeMultiple;
                Repeat = surface.Repeat;
                Center = surface.Center;
            }

            public static void Save(AnimatedTextSurface surface, string file)
            {
                var animation = new Serialized(surface);
                Serializer.Save(animation, file, new Type[] { typeof(List<TextSurfaceBasic>), typeof(Font) });
            }

            public static AnimatedTextSurface Load(string file)
            {
                var animation = Serializer.Load<Serialized>(file, new Type[] { typeof(List<TextSurfaceBasic>), typeof(Font) });
                return Get(animation);
            }

            public static AnimatedTextSurface Get(Serialized serializedObject)
            {
                Font font;

                // Try to find font
                if (Engine.Fonts.ContainsKey(serializedObject.FontName))
                    font = Engine.Fonts[serializedObject.FontName].GetFont(serializedObject.FontSize);
                else
                    font = Engine.DefaultFont;

                var animationSurface = new AnimatedTextSurface(serializedObject.Name, serializedObject.Width, serializedObject.Height, font);
                animationSurface.Frames = new List<TextSurfaceBasic>(serializedObject.Frames);
                animationSurface.UpdateFrameReferences();
                animationSurface.AnimationDuration = serializedObject.AnimationDuration;
                animationSurface.Repeat = serializedObject.Repeat;
                animationSurface.Center = serializedObject.Center;
                return animationSurface;
            }
        }
    }
    
}
