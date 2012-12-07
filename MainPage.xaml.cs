//-----------------------------------------------------------------------
// <copyright file="MainPage.xaml.cs" company="Studio Arcade Ltd">
// Copyright © Studio Arcade Ltd 2012.
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// This code is made available under the Ms-PL or GPL as appropriate.
// Please see LICENSE.txt for more details
// </copyright>
// <Author>Matt Lacey</Author>
// <Author>Laurie Brown</Author>
//
// Node Garden has been extended to Note Garden by
// <Author>keyboardP</Author>
//-----------------------------------------------------------------------

namespace NoteGardenSL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Windows.Threading;
    using Microsoft.Phone.Controls;
    using NodeGardenLib;
    using System.Diagnostics;
    using Microsoft.Xna.Framework.Audio;
    using System.Windows.Media.Animation;
    using System.ComponentModel;
    using NoteGardenSL;

  


    /// <summary>
    /// The page displaying the node garden
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// Maximum value for the line width, based on connectedness
        /// </summary>
        private const float StrokeWeightMin = 1.0f;

        /// <summary>
        /// Minimum value for the line width, based on connectedness
        /// </summary>
        private const float StrokeWeightMax = 7.0f;

        /// <summary>
        /// minimum distance between 2 nodes for a line to be drawn between the 2
        /// </summary>
        private const int MinDist = 300;

        /// <summary>
        /// Number of lines created
        /// </summary>
        private const int NumberOfAvailableLines = 150;

        /// <summary>
        /// Timer used to trigger periodic node movement
        /// </summary>
        private readonly DispatcherTimer nodeMovementTimer = new DispatcherTimer();

        /// <summary>
        /// Looks after the garden, detects events and manages communication between gardens/devices
        /// </summary>
        private Gardener gardener;

        /// <summary>
        /// Random number generator
        /// </summary>
        private Random rand = new Random();

        /// <summary>
        /// All the nodes used in the visualization
        /// </summary>
        private List<VisualNode> nodes;

        /// <summary>
        /// All the lines used to connect nodes
        /// </summary>
        private List<Line> lines;


        readonly DispatcherTimer timer = new DispatcherTimer();

        /// <summary>
        /// This is used for the cue marker to move across and detect any collision
        /// </summary>
        private DispatcherTimer cueTimer = new DispatcherTimer();

        //cue marker line
        private Line cueLine = null;

        /// <summary>
        /// Hold all the sound effects in memory since there aren't too many  
        /// </summary>
        private SoundEffect[] allNotes = new SoundEffect[8];

        /// <summary>
        /// Background images
        /// </summary>
        ImageBrush white = new ImageBrush() { ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("images/scorebackground.png", UriKind.Relative)), Stretch = Stretch.Fill };
        ImageBrush black = new ImageBrush() { ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("images/iscorebackground.png", UriKind.Relative)), Stretch = Stretch.Fill };



        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;

            this.timer.Tick += (s, e) => { Microsoft.Xna.Framework.FrameworkDispatcher.Update(); };
            this.timer.Interval = TimeSpan.FromMilliseconds(100);
            this.timer.Start();

            //create cur marker instance
            cueLine = new Line();
            //ensure that the marker is at the very "front" of the screen, on top of all other elements
            System.Windows.Controls.Canvas.SetZIndex(cueLine, 100);
            cueLine.StrokeThickness = 2.0d;
            cueLine.Stroke = new SolidColorBrush(Colors.Black);
            cueLine.X1 = 0;
            cueLine.Y1 = 0;
            cueLine.X2 = 0;
            cueLine.Y2 = 1280;
            MainCanvas.Children.Add(cueLine);

            this.cueTimer.Tick += cueTimer_Tick;
            this.cueTimer.Interval = TimeSpan.FromMilliseconds(100);
            this.cueTimer.Start();

            //change the background of the main canvas to the image
            MainCanvas.Background = white;


            this.DisplayedVersionNumber.Text = Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
        }

        /// <summary>
        /// This could be any special note. In this case, it plays all the notes and is called when the user's own node is hit
        /// </summary>
        SoundEffect specialNote = SoundEffect.FromStream(Microsoft.Xna.Framework.TitleContainer.OpenStream("notes/whole.wav"));

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {

            //middle C is added manually as mc.wav is not the in the same format as the rest
            //CreateInstance(); has been commented out as they don't allow for sound overlap. Uncomment for SoundEffectInstances rather than SoundEffect 
            allNotes[0] = SoundEffect.FromStream(Microsoft.Xna.Framework.TitleContainer.OpenStream("notes/mc.wav"));//.CreateInstance();

            //load all the wav files (title a.wav, b.wav etc...)
            for (int i = 1; i < 8; i++)
            {
                //load all the other notes A,B,C,D,E,F,G
                char note = (char)(i + 64);

                allNotes[i] = SoundEffect.FromStream(Microsoft.Xna.Framework.TitleContainer.OpenStream(String.Format("notes/{0}.wav", note.ToString())));//.CreateInstance();
            }


        }

        //Posisition of the cue marker line (0 = far left)
        private int cueX = 0;


        /// <summary>
        /// Simply plays the relevant note. Separate method as if there is any additional processing to be done, it can be done here for all notes
        /// </summary>
        /// <param name="i">Index of sound to play</param>
        private void Play(int i)
        {
            allNotes[i].Play();
        }



        /// <summary>
        /// Play the sound of the note provided
        /// </summary>
        /// <param name="sn">Note to play</param>
        /// <param name="connectionStrength">Strength of the connections at this node</param>
        private void PlaySound(SoundNotes sn, float connectionStrength = 0.0f)
        {
            //ConnectionStrength could be used with SoundEffectInstances to change the pitch/volume depending on strength
            //e.g. allNotes[1].Pitch = Math.Min(connectionStrength, 1.0f);    

            switch (sn)
            {
                //simply play the relevant index from the allNotes array
                case SoundNotes.A:
                    Play(1);
                    break;
                case SoundNotes.B:
                    Play(2);
                    break;
                case SoundNotes.C:
                    Play(3);
                    break;
                case SoundNotes.D:
                    Play(4);
                    break;
                case SoundNotes.E:
                    Play(5);
                    break;
                case SoundNotes.F:
                    Play(6);
                    break;
                case SoundNotes.G:
                    Play(7);
                    break;
                case SoundNotes.MiddleC:
                    Play(0);
                    break;
                default:
                    Play(0);
                    break;

            }


        }

        /// <summary>
        /// JUMP relates to how fast the marker moves across the screen (essentially the tempo)
        /// </summary>
        private const int JUMP = 10;

        /// <summary>
        /// Holds the node the user can control. 
        /// </summary>
        private VisualNode selfNode = null;

        void cueTimer_Tick(object sender, EventArgs e)
        {
            //If the user's node has been initialised, move the marker
            if (selfNode != null)
            {
                cueX += JUMP;
                cueLine.X1 += JUMP;
                cueLine.X2 += JUMP;

                //if it goes off screen, randomise the notes for each node and reset the marker back to the beginning
                if (cueX >= 480)
                {
                    RandomiseNotes();

                    cueX = 0;
                    cueLine.X1 = 0;
                    cueLine.X2 = 0;
                }



                //get all nodes at that position
                //KVPair used because the actual X position of node (CurrentX) shouldn't be changed.
                //KVPair calculates to the nearest JUMP value and assigns it to the node
                List<KeyValuePair<VisualNode, int>> notesHit = nodePositionAndNote.Where(x => x.Value == cueX).ToList();

                //iterate through the nodes and play their note
                for (int i = 0; i < notesHit.Count(); i++)
                {

                    VisualNode vn = (notesHit.ElementAt(i).Key);

                    //Could play  different instrument here if the marker has hit the user's node
                    //e.g. if (vn == selfNode) specialSoundEffect.Play();
                    //in this case, play the musical scale if the user's node is hit
                    if (vn == selfNode) specialNote.Play();

                    //play the sound and pass in how "connected" this note is
                    PlaySound(vn.Note, vn.Connectedness);

                    //Play the colour animation for this note
                    AnimateNote(vn);

                }

            }

        }


        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">An object that contains the event data.</param>
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (this.gardener != null)
            {
                this.gardener.Reconnect();
            }

            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Called just before a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">An object that contains the event data.</param>
        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (this.gardener != null)
            {
                this.gardener.Dispose();
            }
        }

        /// <summary>
        /// The start button was clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StartClicked(object sender, RoutedEventArgs e)
        {
            this.gardener = new Gardener();


            var comm = CommType.Udp;

            if (this.WebComms.IsChecked ?? false)
            {
                comm = CommType.Web;
            }

            // Define the way the gardener should behave
            var settings = new GardenerSettings
            {
                CommType = comm,
                //EnableColorDetection = this.EnableColorDetection.IsChecked ?? false,
                //EnableImageDetection = this.EnableImageDetection.IsChecked ?? false,
                EnableShakeDetection = this.EnableShakeDetection.IsChecked ?? false,
               // EnableNoiseDetection = this.EnableNoiseDetection.IsChecked ?? false,
                //NoiseThreshold = 1500,
                //NoiseDuration = 2,
                //ColorToDetect = Colors.Blue,
                //ColorDetectionThreshold = 40,

                // These two properties must be set like this for the camera to be
                // able to be used to do color and image detection
                ViewFinderBrush = this.viewfinderBrush,
                ViewFinderBrushTransform = this.viewfinderBrushTransformation,
            };

            this.gardener.Initialize(settings);


            this.AddHandlersForDetectedEvents();

            // Handle notification, from another device, that a node has been added/changed
            this.gardener.OnNodeChanged += nodeId => this.Dispatcher.BeginInvoke(() =>
            {
                var gardenerNode = this.gardener.Nodes.FirstOrDefault(n => n.Id == nodeId);

                var visualNode = this.nodes.FirstOrDefault(n => n.Id == nodeId);

                if (gardenerNode != null)
                {
                    if (visualNode == null)
                    {
                        var vn = new VisualNode(rand, MainCanvas)
                        {
                            X = gardenerNode.X,
                            Y = gardenerNode.Y,
                            Tag = gardenerNode.Tag,
                            Id = gardenerNode.Id,
                            DisableVirtualMovement = true
                        };

                        this.nodes.Add(vn);
                    }
                    else
                    {
                        var idx = this.nodes.IndexOf(visualNode);

                        this.nodes[idx].X = gardenerNode.X;
                        this.nodes[idx].Y = gardenerNode.Y;
                        this.nodes[idx].Tag = gardenerNode.Tag;
                    }
                }
                else
                {
                    if (visualNode != null)
                    {
                        visualNode.Remove(this.MainCanvas);
                        this.nodes.Remove(visualNode);
                    }
                }
            });

            this.ConfigureOtherOptions();

            this.nodes = new List<VisualNode>();

            this.CreateOwnNode();

            this.gardener.WhereIsEveryBody();

            this.CreateDefaultNodes(Convert.ToInt32(Math.Floor(this.AdditionalNodesCount.Value)));



            this.CreateLines();

            //get hold of the user's node
            selfNode = this.nodes.FirstOrDefault(n => n.Id == gardener.GetSelfNodeId());

            //add all nodes to dictionary
            nodePositionAndNote = this.nodes.ToDictionary(x => x, x => ((int)Math.Round(x.CurrentX / (double)JUMP) * JUMP));



            this.nodeMovementTimer.Interval = TimeSpan.FromMilliseconds(10);
            this.nodeMovementTimer.Tick += this.UpdateDisplay;
            this.nodeMovementTimer.Start();
        }

        /// <summary>
        /// Creates the default nodes.
        /// </summary>
        /// <param name="numberOfNodes">The number of nodes.</param>
        private void CreateDefaultNodes(int numberOfNodes)
        {
            for (int i = 0; i < numberOfNodes; i++)
            {
                this.nodes.Add(new VisualNode(this.rand, MainCanvas));
            }
        }

        /// <summary>
        /// Creates the lines.
        /// </summary>
        private void CreateLines()
        {
            this.lines = new List<Line>(NumberOfAvailableLines);

            for (int i = 0; i < NumberOfAvailableLines; i++)
            {
                this.lines.Add(new Line());
                this.lines[i].StrokeThickness = 2.5f;
              //  this.lines[i].Stroke = new SolidColorBrush(Colors.Black);
                this.lines[i].X1 = 50;
                this.lines[i].Y1 = (i * 7) % 720;
                this.lines[i].X2 = 400;
                this.lines[i].Y2 = (i * 5) % 720;

                MainCanvas.Children.Add(this.lines[i]);
            }
        }

       
        private Dictionary<VisualNode, int> nodePositionAndNote = new Dictionary<VisualNode, int>();

        /// <summary>
        /// Updates the display.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void UpdateDisplay(object sender, EventArgs e)
        {
            int currentLine = 0;

            // hide lines in case they aren't used
            foreach (var t in this.lines)
            {
                t.X1 = -1;
                t.Y1 = -1;
                t.X2 = -1;
                t.Y2 = -1;
            }

            for (int i = 0; i < this.nodes.Count; ++i)
            {
                // update the node's position
                this.nodes[i].Update(MainCanvas.RenderSize);

                for (int j = 0; j < this.nodes.Count; ++j)
                {
                    //update positions + notes (and round position to nearest JUMP)
                    //this rounds the note's X position to a multiple of JUMP so that it can be hit precisely.
                    //Otherwise, if the marker went from 10 to 20, anything between 11 and 19, inclusive, would be  missed
                    nodePositionAndNote[this.nodes[j]] = ((int)Math.Round(this.nodes[j].CurrentX / (double)JUMP) * JUMP);


                    //add each nodes X position and sound int
                    if (i != j)
                    {
                        // calculate the distance between each 2 nodes
                        float distance =
                            (float)Math.Sqrt(
                                Math.Pow(this.nodes[j].CurrentY - this.nodes[i].CurrentY, 2)
                                + Math.Pow(this.nodes[j].CurrentX - this.nodes[i].CurrentX, 2));

                        // if distance is within the threshold
                        if (distance < MinDist)
                        {
                            // add a mapped value between 1-0 to each node's connectedness value
                            float connectedness = VisualNode.Map(distance, 0, MinDist, 1, 0);
                            this.nodes[i].Connectedness += connectedness;
                            this.nodes[j].Connectedness += connectedness;

                            #region Lines are not drawn in Note Garden, so commented out
                            //if (distance > Math.Max(this.nodes[i].NodeSize, this.nodes[j].NodeSize))
                            //{
                            //    // draw a line between 2 nodes. The thickness/alpha varies depending on distance
                            //    this.lines[currentLine].StrokeThickness = VisualNode.Map(distance, 0, MinDist, StrokeWeightMax, StrokeWeightMin);
                            //    this.lines[currentLine].Stroke = new SolidColorBrush(Color.FromArgb((byte)VisualNode.Map(distance, 0, MinDist, 255, 0), 200, 200, 200)); ;//new SolidColorBrush(Color.FromArgb((byte)VisualNode.Map(distance, 0, MinDist, 255, 0), 255, 255, 255));
                            //    this.lines[currentLine].X1 = this.nodes[i].CurrentX;
                            //    this.lines[currentLine].Y1 = this.nodes[i].CurrentY;
                            //    this.lines[currentLine].X2 = this.nodes[j].CurrentX;
                            //    this.lines[currentLine].Y2 = this.nodes[j].CurrentY;

                            //    // switch the currently used line in the pool
                            //    currentLine = (currentLine + 1) % NumberOfAvailableLines;

                            //}
                            #endregion
                        }
                    }
                }

                // draw the node using the Connectedness
                this.nodes[i].Draw();
            }


        }

        /// <summary>
        /// Configures the other options defined before starting the app.
        /// </summary>
        private void ConfigureOtherOptions()
        {
            // This is commented as it relates to a UIElement that is commented out on the page
            ////if (this.ShowVisualDebugger.IsChecked ?? false)
            ////{
            ////    this.CameraView.Opacity = 1;
            ////}

            this.DebugConfigOptions.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Changes the notes for every node to a random one
        /// </summary>
        private void RandomiseNotes()
        {
            //Iterate through all the nodes and assign them a new random note
            foreach (VisualNode vn in this.nodes)
            {

                int noteIndex = rand.Next(65, 73);
                char note = (char)noteIndex;

                if (note != 'H') //if the value is H, then use Middle C
                {
                    try
                    {
                        vn.Note = (SoundNotes)Enum.Parse(typeof(SoundNotes), note.ToString(), true);
                    }
                    catch
                    {
                        //if there was an issue with the note parsing, just assign middle C
                        vn.Note = SoundNotes.MiddleC;
                    }
                }
                else
                {
                    vn.Note = SoundNotes.MiddleC;
                }

            }
        }


       

        /// <summary>
        /// Adds the handlers for detected events.
        /// </summary>
        private void AddHandlersForDetectedEvents()
        {
            this.gardener.OnShakeDetected += () => this.Dispatcher.BeginInvoke(
                () =>
                {
                    // This is where you could do something in response to a shake being detected
                    
                    // If the device is shaken, you could randomise the notes
                   // RandomiseNotes();

                    //when shaken, not stirred, change the colour scheme
                    //if the background is white, change it to black and turn the nodes and marker white
                    if (MainCanvas.Background == white)
                    {
                        cueLine.Stroke = new SolidColorBrush(Colors.White);
                        cueLine.Visibility = System.Windows.Visibility.Visible;
                       
                        MainCanvas.Background = black;
                        foreach (VisualNode vn in this.nodes)
                        {
                            vn.noteNode.notePath.Fill = new SolidColorBrush(Colors.White);
                        }
                       
                    }
                    else if (MainCanvas.Background == black) //change background to null
                    {
                      
                        //hide the cueline so that it doesn't interfere with the colours of the notes (optional)
                        cueLine.Visibility = System.Windows.Visibility.Collapsed;
                        //make all the note 'invisible' (simply turn them black as the background is black);
                        foreach (VisualNode vn in this.nodes)
                        {
                            vn.noteNode.notePath.Fill = new SolidColorBrush(Colors.Black);
                        }
                        MainCanvas.Background = null;

                    }
                    else
                    {
                        cueLine.Stroke = new SolidColorBrush(Colors.Black);
                        cueLine.Visibility = System.Windows.Visibility.Visible;
                        MainCanvas.Background = white;
                        foreach (VisualNode vn in this.nodes)
                        {
                            vn.noteNode.notePath.Fill = new SolidColorBrush(Colors.Black);
                        }
                    }

                });

            this.gardener.OnNoiseDetected += () => this.Dispatcher.BeginInvoke(
                () =>
                {
                    // This is where you could do something in response to a noise being detected
                });

            this.gardener.OnColorDetected += () => this.Dispatcher.BeginInvoke(
                () =>
                {
                    // This is where you could do something in response to a colour being detected with the camera

                });

            //this.gardener.OnImageDetected += img => this.Dispatcher.BeginInvoke(
            //    () =>
            //    {
            //        // This is where you could do something in response to an image being detected
            //    });
        }

        /// <summary>
        /// Creates the node that can primarily be interacted with and will be reflected on other devices.
        /// </summary>
        private void CreateOwnNode()
        {
            var myNode = new MyVisualNode(this.rand, MainCanvas, this.gardener);

            this.gardener.AddSelfNode(myNode.X, myNode.Y);

            myNode.Id = this.gardener.GetSelfNodeId();

            this.nodes.Add(myNode);
        }

        #region Obsolete animation code
        /*
        /// <summary>
        /// (Obsolete)Animate the notes (used for ellipses)
        /// </summary>
        /// <param name="vn">Note to animate</param>
        /// <param name="i"></param>
        private void AnimateEllipseColour(VisualNode vn, int i = 0)
        {

            Duration duration = new Duration(TimeSpan.FromMilliseconds(800));
            ColorAnimation ca = new ColorAnimation();
            ca.Duration = duration;
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            sb.Children.Add(ca);


            if (i >= 3) i = 0;
            //animate each segment of the ellipse
            if (i == 0)
                Storyboard.SetTarget(ca, vn.Center);
            else if (i == 1)
                Storyboard.SetTarget(ca, vn.Shadow1);
            else
                Storyboard.SetTarget(ca, vn.Shadow2);

            Storyboard.SetTargetProperty(ca, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));


            ca.To = GetRandomColour();//Colors.Green;
            sb.AutoReverse = true;

            // Make the Storyboard a resource.
            if (!MainCanvas.Resources.Contains("animate_center"))
                MainCanvas.Resources.Add("animate_center", sb);

            // Begin the animation.
            sb.Completed += (s, o) =>
            {
                i++;
                if (i < 3)
                    AnimateEllipseColour(vn, ++i);
            };
            sb.Begin();
        }
         */
        #endregion

        /// <summary>
        /// Animate the note's colour
        /// </summary>
        /// <param name="vn">The note to animate</param>
        public void AnimateNote(VisualNode vn)
        {
            //create a colour animation as the set duration to 800ms
            Duration duration = new Duration(TimeSpan.FromMilliseconds(800));
            ColorAnimation ca = new ColorAnimation();
            ca.Duration = duration;
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            sb.Children.Add(ca);

            //set the target to be the note path and the property to be "Fill"
            Storyboard.SetTarget(ca, vn.noteNode.notePath);
            Storyboard.SetTargetProperty(ca, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));

            //animate it to a random colour
            ca.To = GetRandomColour();

            //once animated, return back to its original colour (black by default)
            sb.AutoReverse = true;


            // Make the Storyboard a resource.
            if (!MainCanvas.Resources.Contains("animate_note"))
                MainCanvas.Resources.Add("animate_note", sb);

            //start the animation
            sb.Begin();


        }

        Random rnd = new Random();

        //Uses reflection to get the colors from System.Windows.Media.Color enum
        private Color GetRandomColour()
        {
            var colorProperties = typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.Public);
            var colors = colorProperties.Select(prop => (Color)prop.GetValue(null, null));


            int index = rnd.Next(colors.Count());

            return colors.ElementAt(index);
        }
    }
}