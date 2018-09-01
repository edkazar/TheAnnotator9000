using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TheAnnotator9000
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {
        private ESDRSGenerator g_ESDRSGenerator;
        private GestureAnnotation g_CurrentGesture;
        private GestureShape g_CurrentShape;
        private GestureMovement g_CurrentMovement;

        private Type g_VariableType;
        private Type g_PredicateType;
        private Type g_MappingType;
        private Type g_SpatiotemporalType;

        private JArray g_GestureDatabase;
        private JArray g_AnnotatedGesturesID;
        private StorageFolder g_RootFolder;
        private MediaPlayer g_MediaPlayer;
        private string g_SelectedGestureID;

        private int g_GestureCounter;
        private string g_CurrentArm;

        private bool g_RightExists;
        private bool g_LeftExists;
        private int g_NumPlaceholders;
        private int g_CurrentPlaceholder;

        private bool g_FirstTimeIndVar;

        public MainPage()
        {
            this.InitializeComponent();

            g_ESDRSGenerator = new ESDRSGenerator();

            g_VariableType = typeof(Variable);
            g_PredicateType = typeof(Predicate);
            g_MappingType = typeof(Mapping);
            g_SpatiotemporalType = typeof(Spatiotemporal);

            GreetingMessageText.Visibility = Visibility.Visible;
            UsernameTextbox.Visibility = Visibility.Visible;
            PasswordTextbox.Visibility = Visibility.Visible;
            ButtonLogin.Visibility = Visibility.Visible;

            GestureIDTextbox.Visibility = Visibility.Collapsed;
            GestureIDButton.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction1.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction2.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction3.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction4.Visibility = Visibility.Collapsed;
            GestureIDTextInstruction5.Visibility = Visibility.Collapsed;

            g_RootFolder = ApplicationData.Current.LocalFolder;
            Debug.WriteLine(g_RootFolder.Path);
            g_MediaPlayer = new MediaPlayer();
            g_MediaPlayer.IsLoopingEnabled = true;

            g_GestureCounter = 0;

            g_CurrentArm = "";

            g_RightExists = false;
            g_LeftExists = false;

            g_FirstTimeIndVar = true;

            JArray g_AnnotatedGesturesID = new JArray();

            //IndVarComboBox.Items.Add("Ejemplo 2");
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Super secure password encryption
            if(UsernameTextbox.Text == "Edkazar" && PasswordTextbox.Password == "jarom217")
            {
                GreetingMessageText.Visibility = Visibility.Collapsed;
                UsernameTextbox.Visibility = Visibility.Collapsed;
                PasswordTextbox.Visibility = Visibility.Collapsed;
                ButtonLogin.Visibility = Visibility.Collapsed;

                GestureIDTextbox.Visibility = Visibility.Visible;
                GestureIDButton.Visibility = Visibility.Visible;
                GestureIDTextInstruction1.Visibility = Visibility.Visible;
                GestureIDTextInstruction2.Visibility = Visibility.Visible;
                GestureIDTextInstruction3.Visibility = Visibility.Visible;
                GestureIDTextInstruction4.Visibility = Visibility.Visible;
                GestureIDTextInstruction5.Visibility = Visibility.Visible;

                loadGestureDatabase();
                loadGesturesIDsFile();
            }
        }

        private void GestureIDButton_Click(object sender, RoutedEventArgs e)
        {
            if (GestureIDTextbox.Text != "")
            {
                ErrorMessageText.Visibility = Visibility.Collapsed;

                JObject selectedGesture = null;

                foreach (JObject obj in g_GestureDatabase)
                {
                    if (obj["Gesture Code"].ToString() == GestureIDTextbox.Text)
                    {
                        selectedGesture = obj;
                        break;
                    }
                }

                if(selectedGesture != null)
                {
                    bool startAnnotating = true;

                    foreach (string ID in g_AnnotatedGesturesID)
                    {
                        if (ID == GestureIDTextbox.Text)
                        {
                            RepeatedGestureMessageText.Visibility = Visibility.Visible;
                            startAnnotating = false;
                            break;
                        }
                    }

                    if (startAnnotating)
                    {
                        RepeatedGestureMessageText.Visibility = Visibility.Collapsed;
                        GestureIDTextbox.Visibility = Visibility.Collapsed;
                        GestureIDButton.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction1.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction2.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction3.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction4.Visibility = Visibility.Collapsed;
                        GestureIDTextInstruction5.Visibility = Visibility.Collapsed;
                        g_SelectedGestureID = selectedGesture["Gesture Code"].ToString();
                        GestureIDText.Text += g_SelectedGestureID;
                        GestureIDText.Visibility = Visibility.Visible;
                        GestureRecording.Visibility = Visibility.Visible;
                        RightText.Visibility = Visibility.Visible;
                        LeftText.Visibility = Visibility.Visible;
                        SpeechTranscriptionText.Visibility = Visibility.Visible;

                        gestureVideoPlayback(selectedGesture);
                        startGestureAnnotation();
                    }

                }
                else
                {
                    GestureIDTextbox.Visibility = Visibility.Visible;
                    GestureIDButton.Visibility = Visibility.Visible;
                    ErrorMessageText.Visibility = Visibility.Visible;
                }
            }
        }

        private async void loadGestureDatabase()
        {
            string filepath = @"JSONs\testJSON2.json";
            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            var data = await FileIO.ReadTextAsync(file);
            g_GestureDatabase = JArray.Parse(data);
        }

        private async void gestureVideoPlayback(JObject pSelectedGesture)
        {
            string filepath = @"Videos\";
            filepath = filepath + pSelectedGesture["Gesture Code"].ToString()[0];
            filepath = filepath + pSelectedGesture["Gesture Code"].ToString()[0];
            filepath += pSelectedGesture["Gesture Code"].ToString()[2] + ".avi";

            StorageFile file = await g_RootFolder.GetFileAsync(filepath);
            GestureRecording.SetMediaPlayer(g_MediaPlayer);
            g_MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            g_MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Convert.ToInt32(pSelectedGesture["Gesture Spotted in Color at"]) - 2);
            g_MediaPlayer.Play();

            SpeechTranscriptionText.Text = pSelectedGesture["Speech Context"].ToString();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(10);
            timer.Start();
            timer.Tick += (o, args) =>
            {
                timer.Start();
                g_MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Convert.ToInt32(pSelectedGesture["Gesture Spotted in Color at"]) - 2);
            };
        }

        private void startGestureAnnotation()
        {
            TaxonomyAnnotatorGrid.Visibility = Visibility.Visible;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton thisButton = (ToggleButton)sender;
        }

        private Predicate Create1ArityPredicateAux(string pName, string pArgument1)
        {
            List<string> arguments = new List<string>();
            arguments.Add(pArgument1);
            List<Type> sort = new List<Type>();
            sort.Add(typeof(Variable));
            return g_ESDRSGenerator.createPredicate(pName, sort, arguments);
        }

        private Predicate Create2ArityPredicateAux(string pName, string pArgument1, string pArgument2)
        {
            List<string> arguments = new List<string>();
            arguments.Add(pArgument1);
            arguments.Add(pArgument2);
            List<Type> sort = new List<Type>();
            sort.Add(typeof(Variable));
            sort.Add(typeof(Variable));
            return g_ESDRSGenerator.createPredicate(pName, sort, arguments);
        }

        private Predicate Create3ArityPredicateAux(string pName, string pArgument1, string pArgument2, string pArgument3, bool pIsMapping)
        {
            List<string> arguments = new List<string>();
            arguments.Add(pArgument1);
            arguments.Add(pArgument2);
            arguments.Add(pArgument3);
            List<Type> sort = new List<Type>();
            sort.Add(typeof(Variable));
            sort.Add(typeof(Variable));
            if(pIsMapping)
                sort.Add(typeof(Mapping));
            else
                sort.Add(typeof(Variable));
            return g_ESDRSGenerator.createPredicate(pName, sort, arguments);
        }

        private void TaxButton_Click(object sender, RoutedEventArgs e)
        {
            foreach(Object obj in TaxonomyAnnotatorGrid.Children)
            {
                if (obj.GetType() == typeof(ToggleButton))
                {
                    ToggleButton thisButton = (ToggleButton)obj;
                    if (thisButton.IsChecked == true)
                    {
                        if(thisButton.Content.ToString() != "Gesture")
                        {
                            Predicate newPred = Create1ArityPredicateAux(thisButton.Content.ToString(), ConstantValues.g_GestureString + g_SelectedGestureID);
                            g_CurrentGesture.ExtraPredicates.Add(newPred);
                        }
                        else
                        {
                            // THE GESTURE ANNOTATION IS CREATED HERE!
                            g_CurrentGesture = g_ESDRSGenerator.createGestureAnnotation(g_SelectedGestureID);
                            Variable newVar = g_ESDRSGenerator.createVariable(ConstantValues.g_GestureString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                            g_CurrentGesture.Variables.Add(newVar);
                            Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);
                            g_CurrentGesture.ExtraPredicates.Add(newPred);
                        }
                    }
                }
            }
            
            g_CurrentShape = g_ESDRSGenerator.createGestureShape();

            TaxonomyAnnotatorGrid.Visibility = Visibility.Collapsed;
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            g_CurrentArm = ConstantValues.g_RightString;
        }

        private void ShapeCreationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            if(selectedButton.Content.ToString() == "Yes")
            {
                Variable newVar = new Variable();

                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    g_RightExists = true;
                    newVar = g_ESDRSGenerator.createVariable(ConstantValues.g_RightString + ConstantValues.g_GestureString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                }
                else
                {
                    g_LeftExists = true;
                    newVar = g_ESDRSGenerator.createVariable(ConstantValues.g_LeftString + ConstantValues.g_GestureString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());      
                }

                g_CurrentGesture.Variables.Add(newVar);
                Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);
                g_CurrentGesture.ExtraPredicates.Add(newPred);

                ArmPoseText.Visibility = Visibility.Visible;
                ExtendedPoseButton.Visibility = Visibility.Visible;
                SemiExtendedPoseButton.Visibility = Visibility.Visible;
                NoExtendedPoseButton.Visibility = Visibility.Visible;

                RightArmText.Visibility = Visibility.Collapsed;
                LeftArmText.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                if(g_CurrentArm == ConstantValues.g_RightString)
                {
                    RightArmText.Visibility = Visibility.Collapsed;
                    LeftArmText.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_LeftString;
                }
                else
                {
                    LeftArmText.Visibility = Visibility.Collapsed;
                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    AnotherShapeButton.Visibility = Visibility.Visible;
                    ShapeDoneButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void PoseButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            Variable newVar = new Variable();

            if (ArmPoseText.Visibility == Visibility.Visible)
            {
                newVar = g_ESDRSGenerator.createVariable(g_CurrentArm + ConstantValues.g_ArmString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                g_CurrentShape.Variables.Add(newVar);

                ArmPoseText.Visibility = Visibility.Collapsed;
                ArmOrientationText.Visibility = Visibility.Visible;
            }
            else if (HandPoseText.Visibility == Visibility.Visible)
            {
                newVar = g_ESDRSGenerator.createVariable(g_CurrentArm + ConstantValues.g_HandString, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                g_CurrentShape.Variables.Add(newVar);

                Predicate component = Create2ArityPredicateAux(ConstantValues.g_ComponentString, newVar.name, g_CurrentArm + ConstantValues.g_ArmString);
                g_CurrentShape.Component.Add(component);

                HandPoseText.Visibility = Visibility.Collapsed;
                HandOrientationText.Visibility = Visibility.Visible;
            }

            Predicate newPred = Create1ArityPredicateAux(newVar.name, newVar.name);  
            g_CurrentShape.Pose.Add(newPred);

            ExtendedPoseButton.Visibility = Visibility.Collapsed;
            SemiExtendedPoseButton.Visibility = Visibility.Collapsed;
            NoExtendedPoseButton.Visibility = Visibility.Collapsed;

            UpOrientationButton.Visibility = Visibility.Visible;
            DownOrientationButton.Visibility = Visibility.Visible;
            LeftOrientationButton.Visibility = Visibility.Visible;
            RightOrientationButton.Visibility = Visibility.Visible;
            ForwardOrientationButton.Visibility = Visibility.Visible;
        }

        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            Predicate newPred = new Predicate();

            if (ArmOrientationText.Visibility == Visibility.Visible)
            {
                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), ConstantValues.g_RightString + ConstantValues.g_ArmString);
                }
                else
                {
                    newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), ConstantValues.g_LeftString + ConstantValues.g_ArmString);
                }

                ArmOrientationText.Visibility = Visibility.Collapsed;
                HandPoseText.Visibility = Visibility.Visible;
                ExtendedPoseButton.Visibility = Visibility.Visible;
                SemiExtendedPoseButton.Visibility = Visibility.Visible;
                NoExtendedPoseButton.Visibility = Visibility.Visible;
            }
            else if (HandOrientationText.Visibility == Visibility.Visible)
            {
                if (g_CurrentArm == ConstantValues.g_RightString)
                {
                    newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), ConstantValues.g_RightString + ConstantValues.g_HandString);
                }
                else
                {
                    newPred = Create1ArityPredicateAux(ConstantValues.g_OrientationString + selectedButton.Content.ToString(), ConstantValues.g_LeftString + ConstantValues.g_HandString);
                }

                HandOrientationText.Visibility = Visibility.Collapsed;
                LittleFingerText.Visibility = Visibility.Visible;
                FingerDescriptionText.Visibility = Visibility.Visible;
                FingerExtendedPoseButton.Visibility = Visibility.Visible;
                FingerSemiExtendedPoseButton.Visibility = Visibility.Visible;
                FingerNoExtendedPoseButton.Visibility = Visibility.Visible;
            }

            g_CurrentShape.Orientation.Add(newPred);

            UpOrientationButton.Visibility = Visibility.Collapsed;
            DownOrientationButton.Visibility = Visibility.Collapsed;
            LeftOrientationButton.Visibility = Visibility.Collapsed;
            RightOrientationButton.Visibility = Visibility.Collapsed;
            ForwardOrientationButton.Visibility = Visibility.Collapsed;
        }

        private void FingerPoseButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            Variable newVar = new Variable();
            string varName = g_CurrentArm;

            if (LittleFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + LittleFingerText.Text + ConstantValues.g_FingerString;

                LittleFingerText.Visibility = Visibility.Collapsed;
                RingFingerText.Visibility = Visibility.Visible;
            }
            else if (RingFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + RingFingerText.Text + ConstantValues.g_FingerString;

                RingFingerText.Visibility = Visibility.Collapsed;
                MiddleFingerText.Visibility = Visibility.Visible;
            }
            else if (MiddleFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + MiddleFingerText.Text + ConstantValues.g_FingerString;

                MiddleFingerText.Visibility = Visibility.Collapsed;
                IndexFingerText.Visibility = Visibility.Visible;
            }
            else if (IndexFingerText.Visibility == Visibility.Visible)
            {
                varName = varName + IndexFingerText.Text + ConstantValues.g_FingerString;

                IndexFingerText.Visibility = Visibility.Collapsed;
                ThumbFingerText.Visibility = Visibility.Visible;
            }
            else
            {
                varName = varName + ThumbFingerText.Text + ConstantValues.g_FingerString;

                ThumbFingerText.Visibility = Visibility.Collapsed;
                LittleFingerText.Visibility = Visibility.Visible;

                FingerDescriptionText.Visibility = Visibility.Collapsed;
                FingerOrientationText.Visibility = Visibility.Visible;
                FingerExtendedPoseButton.Visibility = Visibility.Collapsed;
                FingerSemiExtendedPoseButton.Visibility = Visibility.Collapsed;
                FingerNoExtendedPoseButton.Visibility = Visibility.Collapsed;

                FingerNoOrientationButton.Visibility = Visibility.Visible;
                FingerUpOrientationButton.Visibility = Visibility.Visible;
                FingerDownOrientationButton.Visibility = Visibility.Visible;
                FingerLeftOrientationButton.Visibility = Visibility.Visible;
                FingerRightOrientationButton.Visibility = Visibility.Visible;
                FingerForwardOrientationButton.Visibility = Visibility.Visible;
            }

            newVar = g_ESDRSGenerator.createVariable(varName, ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
            g_CurrentShape.Variables.Add(newVar);

            Predicate component = Create2ArityPredicateAux(ConstantValues.g_ComponentString, newVar.name, g_CurrentArm + ConstantValues.g_HandString);
            g_CurrentShape.Component.Add(component);

            Predicate newPred = Create1ArityPredicateAux(newVar.name + ConstantValues.g_PoseString + selectedButton.Content.ToString(), newVar.name);
            g_CurrentShape.Pose.Add(newPred);
        }

        private void FingerOrientationButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            string predName = g_CurrentArm;

            if (LittleFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + LittleFingerText.Text + ConstantValues.g_FingerString;

                LittleFingerText.Visibility = Visibility.Collapsed;
                RingFingerText.Visibility = Visibility.Visible;
            }
            else if (RingFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + RingFingerText.Text + ConstantValues.g_FingerString;

                RingFingerText.Visibility = Visibility.Collapsed;
                MiddleFingerText.Visibility = Visibility.Visible;
            }
            else if (MiddleFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + MiddleFingerText.Text + ConstantValues.g_FingerString;

                MiddleFingerText.Visibility = Visibility.Collapsed;
                IndexFingerText.Visibility = Visibility.Visible;
            }
            else if (IndexFingerText.Visibility == Visibility.Visible)
            {
                predName = predName + IndexFingerText.Text + ConstantValues.g_FingerString;

                IndexFingerText.Visibility = Visibility.Collapsed;
                ThumbFingerText.Visibility = Visibility.Visible;
            }
            else
            {
                predName = predName + ThumbFingerText.Text + ConstantValues.g_FingerString;

                LittleFingerText.Visibility = Visibility.Visible;
                RingFingerText.Visibility = Visibility.Visible;
                MiddleFingerText.Visibility = Visibility.Visible;
                IndexFingerText.Visibility = Visibility.Visible;

                FingerOrientationText.Visibility = Visibility.Collapsed;
                FingerSeparationText.Visibility = Visibility.Visible;
                FingerNoOrientationButton.Visibility = Visibility.Collapsed;
                FingerUpOrientationButton.Visibility = Visibility.Collapsed;
                FingerDownOrientationButton.Visibility = Visibility.Collapsed;
                FingerLeftOrientationButton.Visibility = Visibility.Collapsed;
                FingerRightOrientationButton.Visibility = Visibility.Collapsed;
                FingerForwardOrientationButton.Visibility = Visibility.Collapsed;

                LittleFingerSwitch.Visibility = Visibility.Visible;
                RingFingerSwitch.Visibility = Visibility.Visible;
                MiddleFingerSwitch.Visibility = Visibility.Visible;
                IndexFingerSwitch.Visibility = Visibility.Visible;

                DoneSeparatingButton.Visibility = Visibility.Visible;
            }

            Predicate newPred = Create1ArityPredicateAux(predName + ConstantValues.g_OrientationString + selectedButton.Content.ToString(), predName);
            g_CurrentShape.Orientation.Add(newPred);
        }

        private void DoneWithFingersButton_Click(object sender, RoutedEventArgs e)
        {
            createPredforSwitches();

            if (g_CurrentArm == ConstantValues.g_RightString)
            {
                LeftArmText.Visibility = Visibility.Visible;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                g_CurrentArm = ConstantValues.g_LeftString;
            }
            else
            {
                LeftArmText.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
                isDependentText.Visibility = Visibility.Visible;
                YesDependentButton.Visibility = Visibility.Visible;
                NoDependentButton.Visibility = Visibility.Visible;
            }

            FingerSeparationText.Visibility = Visibility.Collapsed;

            LittleFingerText.Visibility = Visibility.Collapsed;
            RingFingerText.Visibility = Visibility.Collapsed;
            MiddleFingerText.Visibility = Visibility.Collapsed;
            IndexFingerText.Visibility = Visibility.Collapsed;
            ThumbFingerText.Visibility = Visibility.Collapsed;

            LittleFingerSwitch.IsOn = false;
            RingFingerSwitch.IsOn = false;
            MiddleFingerSwitch.IsOn = false;
            IndexFingerSwitch.IsOn = false;
            LittleFingerSwitch.Visibility = Visibility.Collapsed;
            RingFingerSwitch.Visibility = Visibility.Collapsed;
            MiddleFingerSwitch.Visibility = Visibility.Collapsed;
            IndexFingerSwitch.Visibility = Visibility.Collapsed;

            DoneSeparatingButton.Visibility = Visibility.Collapsed;
        }

        private void createPredforSwitches()
        {
            if (LittleFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + LittleFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + RingFingerText.Text + ConstantValues.g_FingerString);
                g_CurrentShape.Separation.Add(newPred);
            }

            if (RingFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + RingFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + MiddleFingerText.Text + ConstantValues.g_FingerString);
                g_CurrentShape.Separation.Add(newPred);
            }

            if (MiddleFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + MiddleFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + IndexFingerText.Text + ConstantValues.g_FingerString);
                g_CurrentShape.Separation.Add(newPred);
            }

            if (IndexFingerSwitch.IsOn)
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_FingerString + ConstantValues.g_SeparatedString,
                    g_CurrentArm + IndexFingerText.Text + ConstantValues.g_FingerString,
                    g_CurrentArm + ThumbFingerText.Text + ConstantValues.g_FingerString);
                g_CurrentShape.Separation.Add(newPred);
            }
        }

        private void OptionDependentButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;

            if (selectedButton.Content.ToString() == "Yes")
            {
                Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_DependenceString,
                    ConstantValues.g_RightString + ConstantValues.g_GestureString, ConstantValues.g_LeftString + ConstantValues.g_GestureString);
                g_CurrentShape.Dependence = newPred;
            }

            g_CurrentGesture.Shape.Add(g_CurrentShape);
            g_CurrentShape = g_ESDRSGenerator.createGestureShape();

            isDependentText.Visibility = Visibility.Collapsed;
            YesDependentButton.Visibility = Visibility.Collapsed;
            NoDependentButton.Visibility = Visibility.Collapsed;

            AnotherShapeButton.Visibility = Visibility.Visible;
            ShapeDoneButton.Visibility = Visibility.Visible;
        }

        private void AnotherShapeButton_Click(object sender, RoutedEventArgs e)
        {
            RightArmText.Visibility = Visibility.Visible;
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            g_CurrentArm = ConstantValues.g_RightString;

            AnotherShapeButton.Visibility = Visibility.Collapsed;
            ShapeDoneButton.Visibility = Visibility.Collapsed;
        }

        private void ShapeDoneButton_Click(object sender, RoutedEventArgs e)
        {
            AnotherShapeButton.Visibility = Visibility.Collapsed;
            ShapeDoneButton.Visibility = Visibility.Collapsed;

            g_CurrentMovement = g_ESDRSGenerator.createGestureMovement();
            
            if (g_RightExists)
            {
                RightPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                g_CurrentArm = ConstantValues.g_RightString;
            }
            else if (g_LeftExists)
            {
                LeftPlaceholdersText.Visibility = Visibility.Visible;
                NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                g_CurrentArm = ConstantValues.g_LeftString;
            }
            else
            {
                DoneTrajectoryButton.Visibility = Visibility.Visible;
            }
        }

        private void DoneNumPlaceholdersButton_Click(object sender, RoutedEventArgs e)
        {
            if(NumPlaceholdersTextbox.Text != "")
            {
                ErrorTrajectoryMessageText.Visibility = Visibility.Collapsed;

                g_NumPlaceholders = Int32.Parse(NumPlaceholdersTextbox.Text);
                g_CurrentPlaceholder = 1;
                bool firstPoint = true;

                for (int counter = 0; counter < g_NumPlaceholders - 1; counter++)
                {
                    //Assuming we are getting the Spationtemporal info. I neeed to do this.

                    if (firstPoint)
                    {
                        Spatiotemporal firstPlaceholderPoint = g_ESDRSGenerator.createSpatiotemporal(g_CurrentArm + ConstantValues.g_PlaceholderPointString + (counter + 1).ToString(), 0, 0, 0, new TimeSpan());
                        g_CurrentMovement.Points.Add(firstPlaceholderPoint);
                        firstPoint = false;
                    }
                    
                    Spatiotemporal newPlaceholderPoint = g_ESDRSGenerator.createSpatiotemporal(g_CurrentArm + ConstantValues.g_PlaceholderPointString + (counter + 2).ToString(), 0, 0, 0, new TimeSpan());
                    g_CurrentMovement.Points.Add(newPlaceholderPoint);

                    Variable newTraj = g_ESDRSGenerator.createVariable(g_CurrentArm + ConstantValues.g_TrajectoryString + (counter + 1).ToString(), ConstantValues.g_IndividualString, new TimeSpan(), new TimeSpan());
                    g_CurrentMovement.Variables.Add(newTraj);
                    Predicate newPred = Create2ArityPredicateAux(ConstantValues.g_TrajectoryString, g_CurrentArm + g_CurrentMovement.Points[counter].name, g_CurrentArm + g_CurrentMovement.Points[counter + 1].name);
                    g_CurrentMovement.Trajectories.Add(newPred);
                }

                LeftPlaceholdersText.Visibility = Visibility.Collapsed;
                RightPlaceholdersText.Visibility = Visibility.Collapsed;
                NumPlaceholdersTextbox.Visibility = Visibility.Collapsed;
                DoneNumPlaceholdersButton.Visibility = Visibility.Collapsed;

                MotionPlanesImage.Visibility = Visibility.Visible;
                DescribePlaneOfMotion.Visibility = Visibility.Visible;
                MotionCoronalButton.Visibility = Visibility.Visible;
                MotionSagittalButton.Visibility = Visibility.Visible;
                MotionTransverseButton.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorTrajectoryMessageText.Visibility = Visibility.Visible;
            }
        }

        private void MotionOptionButton_Click(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;

            Predicate newPred = Create1ArityPredicateAux(g_CurrentArm + ConstantValues.g_MainPlaneString + selectedButton.Content.ToString(), g_CurrentArm + ConstantValues.g_GestureString);
            g_CurrentMovement.MainPlane = newPred;

            DescribePlaneOfMotion.Visibility = Visibility.Collapsed;
            MotionCoronalButton.Visibility = Visibility.Collapsed;
            MotionSagittalButton.Visibility = Visibility.Collapsed;
            MotionTransverseButton.Visibility = Visibility.Collapsed;

            char[] chars = DescribeTrajectoryText.Text.ToCharArray();
            chars[chars.Length - 1] = Convert.ToChar(g_CurrentPlaceholder.ToString());
            DescribeTrajectoryText.Text = new string(chars);
            DescribeTrajectoryText.Visibility = Visibility.Visible;
            MotionDirectionsGrid.Visibility = Visibility.Visible;
        }    

        private void DoneMotioningOptionButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Object obj in MotionDirectionsGrid.Children)
            {
                if (obj.GetType() == typeof(ToggleButton))
                {
                    ToggleButton thisButton = (ToggleButton)obj;
                    if (thisButton.IsChecked == true)
                    {
                        // create
                        Predicate newPred = Create1ArityPredicateAux(ConstantValues.g_DirectionString + thisButton.Content, g_CurrentMovement.Trajectories[g_CurrentPlaceholder - 1].name);
                        g_CurrentMovement.Directions.Add(newPred);                        
                    }

                    thisButton.IsChecked = false;
                }
            }
            
            g_CurrentPlaceholder++;

            if (g_CurrentPlaceholder<g_NumPlaceholders)
            {
                char[] chars = DescribeTrajectoryText.Text.ToCharArray();
                chars[chars.Length-1] = Convert.ToChar(g_CurrentPlaceholder.ToString());
                DescribeTrajectoryText.Text = new string(chars);
            }
            else
            {
                DescribeTrajectoryText.Visibility = Visibility.Collapsed;
                MotionDirectionsGrid.Visibility = Visibility.Collapsed;
                MotionPlanesImage.Visibility = Visibility.Collapsed;

                if (g_CurrentArm == ConstantValues.g_RightString && g_LeftExists)
                {
                    NumPlaceholdersTextbox.Text = "";
                    LeftPlaceholdersText.Visibility = Visibility.Visible;
                    NumPlaceholdersTextbox.Visibility = Visibility.Visible;
                    DoneNumPlaceholdersButton.Visibility = Visibility.Visible;
                    g_CurrentArm = ConstantValues.g_LeftString;
                }
                else
                {
                    DoneTrajectoryButton.Visibility = Visibility.Visible;
                }
            }           
        }

        private void DoneTrajectoringButton_Click(object sender, RoutedEventArgs e)
        {
            g_CurrentGesture.Movement = g_CurrentMovement;
            g_CurrentMovement = g_ESDRSGenerator.createGestureMovement();

            DoneTrajectoryButton.Visibility = Visibility.Collapsed;

            if (g_RightExists || g_LeftExists)
            {
                doesExemplifyText.Visibility = Visibility.Visible;
                YesExemplifiesoButton.Visibility = Visibility.Visible;
                NoExemplifiesButton.Visibility = Visibility.Visible;
            }
            else
            {
                DoneExemplifyingButton.Visibility = Visibility.Visible;
            }
        }
/// voy por el exemplifies
        private void OptionExemplifiesButton_OnClick(object sender, RoutedEventArgs e)
        {
            doesExemplifyText.Visibility = Visibility.Collapsed;
            YesExemplifiesoButton.Visibility = Visibility.Collapsed;
            NoExemplifiesButton.Visibility = Visibility.Collapsed;

            Button selectedButton = sender as Button;

            if (selectedButton.Content.ToString() == "Yes")
            {
                if (g_RightExists)
                {
                    ExemplifiesGrid.Visibility = Visibility.Visible;
                    RightExemplefiesText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Right";
                }
                else
                {
                    ExemplifiesGrid.Visibility = Visibility.Visible;
                    LeftExemplefiesText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Left";
                }
            }
            else
            {
                DoneExemplifyingButton.Visibility = Visibility.Visible;
            }
        }

        private void CreateIndVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndividualTextbox.Text != "")
            {
                //create

                IndVarComboBox.Items.Add(IndividualTextbox.Text);
                IndVarComboBox2.Items.Add(IndividualTextbox.Text);
                IndividualTextbox.Text = "";
            }   
        }

        private void DoneAssigningVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndVarComboBox.SelectedItem != null)
            {
                // create

                if (g_CurrentArm == "Right")
                {
                    RightExemplefiesText.Visibility = Visibility.Collapsed;

                    if (g_LeftExists)
                    {
                        LeftExemplefiesText.Visibility = Visibility.Visible;
                        g_CurrentArm = "Left";
                    }
                    else
                    {
                        ExemplifiesGrid.Visibility = Visibility.Collapsed;
                        g_CurrentArm = "Right";

                        DoneExemplifyingButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    LeftExemplefiesText.Visibility = Visibility.Collapsed;
                    ExemplifiesGrid.Visibility = Visibility.Collapsed;

                    DoneExemplifyingButton.Visibility = Visibility.Visible;
                }

                IndVarComboBox.SelectedItem = null;
            }
        }

        private void DoneExemplifyingButton_Click(object sender, RoutedEventArgs e)
        {
            DoneExemplifyingButton.Visibility = Visibility.Collapsed;

            if (g_RightExists || g_LeftExists)
            {
                isSynchroText.Visibility = Visibility.Visible;
                YesSynchroButton.Visibility = Visibility.Visible;
                NoSynchroButton.Visibility = Visibility.Visible;
            }
            else
            {
                DoneSynchronizingButton.Visibility = Visibility.Visible;
            }
        }

        private void OptionSynchroButton_OnClick(object sender, RoutedEventArgs e)
        {
            isSynchroText.Visibility = Visibility.Collapsed;
            YesSynchroButton.Visibility = Visibility.Collapsed;
            NoSynchroButton.Visibility = Visibility.Collapsed;

            Button selectedButton = sender as Button;

            if (selectedButton.Content.ToString() == "Yes")
            {
                ChoooseEventText.Visibility = Visibility.Visible;
                EventVarComboBox.Visibility = Visibility.Visible;
                CreatedEventVar.Visibility = Visibility.Visible;
                CreateIndVarText2.Visibility = Visibility.Visible;
                EventualityTextbox.Visibility = Visibility.Visible;
                CreateEventVarButton.Visibility = Visibility.Visible;
            }
            else
            {
                DoneSynchronizingButton.Visibility = Visibility.Visible;
            }
        }

        private void CreateEventVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventualityTextbox.Text != "")
            {
                selectSynchroEventText.Visibility = Visibility.Visible;
                EventualityTextbox.Visibility = Visibility.Visible;
                CreateIndVarButton2.Visibility = Visibility.Visible;
                IndVarComboBox2.Visibility = Visibility.Visible;
                IndividualTextbox2.Visibility = Visibility.Visible;
                AddIndVarText.Visibility = Visibility.Visible;
                AddIndtoEventButton.Visibility = Visibility.Visible;
                DoneEventVarButton.Visibility = Visibility.Visible;

                CreatedEventVar.Text = EventualityTextbox.Text + "()";
            }
        }

        private void CreateIndVarButton2_Click(object sender, RoutedEventArgs e)
        {
            if (IndividualTextbox2.Text != "")
            {
                IndVarComboBox.Items.Add(IndividualTextbox2.Text);
                IndVarComboBox2.Items.Add(IndividualTextbox2.Text);
                IndividualTextbox2.Text = "";   
            }
        }

        private void AddIndtoEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndVarComboBox2.SelectedItem != null)
            {
                CreatedEventVar.Text = CreatedEventVar.Text.Remove(CreatedEventVar.Text.Length - 1);
                if (!g_FirstTimeIndVar)
                {
                    CreatedEventVar.Text += ",";
                }

                g_FirstTimeIndVar = false;
                CreatedEventVar.Text = CreatedEventVar.Text + IndVarComboBox2.SelectedValue + ")";
                IndVarComboBox2.SelectedItem = null;
            }
        }

        private void DoneEventVarButton_Click(object sender, RoutedEventArgs e)
        {
            if(EventualityTextbox.Text != "")
            {
                EventVarComboBox.Items.Add(EventualityTextbox.Text);
                IndVarComboBox2.SelectedItem = null;
                g_FirstTimeIndVar = true;
                CreatedEventVar.Text = "";
                EventualityTextbox.Text = "";

                selectSynchroEventText.Visibility = Visibility.Collapsed;
                IndividualTextbox2.Visibility = Visibility.Collapsed;
                CreateIndVarButton2.Visibility = Visibility.Collapsed;
                IndVarComboBox2.Visibility = Visibility.Collapsed;
                AddIndVarText.Visibility = Visibility.Collapsed;
                AddIndtoEventButton.Visibility = Visibility.Collapsed;
                DoneEventVarButton.Visibility = Visibility.Collapsed;

                FinishEventVarButton.Visibility = Visibility.Visible;
            }
        }

        private void FinishEventVarButton_Click(object sender, RoutedEventArgs e)
        {
            if (EventVarComboBox.SelectedItem != null)
            {
                //create

                EventVarComboBox.SelectedItem = null;

                ChoooseEventText.Visibility = Visibility.Collapsed;
                EventVarComboBox.Visibility = Visibility.Collapsed;
                CreatedEventVar.Visibility = Visibility.Collapsed;
                CreateIndVarText2.Visibility = Visibility.Collapsed;
                EventualityTextbox.Visibility = Visibility.Collapsed;
                CreateEventVarButton.Visibility = Visibility.Collapsed;
                FinishEventVarButton.Visibility = Visibility.Collapsed;

                DoneSynchronizingButton.Visibility = Visibility.Visible;
            }
        }

        private void DoneSynchronizingButton_Click(object sender, RoutedEventArgs e)
        {
            if (g_RightExists || g_LeftExists)
            {
                YesLocButton.Visibility = Visibility.Visible;
                NoLocButton.Visibility = Visibility.Visible;

                DoneSynchronizingButton.Visibility = Visibility.Collapsed;

                if (g_RightExists)
                {
                    isRightLocText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Right";
                }
                else
                {
                    isLeftLocText.Visibility = Visibility.Visible;
                    g_CurrentArm = "Left";
                }
            }
            else
            {
                DoneSynchronizingButton.Visibility = Visibility.Collapsed;

                DoneLocalizingButton.Visibility = Visibility.Visible;
            }
        }

        private void OptionLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button selectedButton = sender as Button;
            if (selectedButton.Content.ToString() == "Yes")
            {
                isRightLocText.Visibility = Visibility.Collapsed;
                isLeftLocText.Visibility = Visibility.Collapsed;
                YesLocButton.Visibility = Visibility.Collapsed;
                NoLocButton.Visibility = Visibility.Collapsed;

                EventVarComboBox.Visibility = Visibility.Visible;
                IndVarComboBox2.Visibility = Visibility.Visible;
                SelectPhysicalLocText.Visibility = Visibility.Visible;
                PhysicalLocTextbox.Visibility = Visibility.Visible;
                PhysicalLocComboBox.Visibility = Visibility.Visible;
                SelectIndividualText.Visibility = Visibility.Visible;
                CreateLocButton.Visibility = Visibility.Visible;
                AssignLocButton.Visibility = Visibility.Visible;

                if(g_CurrentArm == "Right")
                {
                    LocforRightText.Visibility = Visibility.Visible;
                }
                else
                {
                    LocforLeftText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                LocforRightText.Visibility = Visibility.Collapsed;
                LocforLeftText.Visibility = Visibility.Collapsed;
                isRightLocText.Visibility = Visibility.Collapsed;
                isLeftLocText.Visibility = Visibility.Collapsed;
                YesLocButton.Visibility = Visibility.Collapsed;
                NoLocButton.Visibility = Visibility.Collapsed;

                if (g_CurrentArm == "Right")
                {
                    LocforLeftText.Visibility = Visibility.Visible;
                    YesLocButton.Visibility = Visibility.Visible;
                    NoLocButton.Visibility = Visibility.Visible;

                    g_CurrentArm = "Left";
                }
                else
                {
                    

                    DoneLocalizingButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void CreateLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(PhysicalLocTextbox.Text != "")
            {
                PhysicalLocComboBox.Items.Add(PhysicalLocTextbox.Text);

                PhysicalLocTextbox.Text = "";
            }
        }

        private void AssignLocButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (EventVarComboBox.SelectedItem != null && IndVarComboBox2.SelectedItem != null && PhysicalLocComboBox.SelectedItem != null)
            {
                EventVarComboBox.SelectedItem = null;
                IndVarComboBox2.SelectedItem = null;
                PhysicalLocComboBox.SelectedItem = null;

                EventVarComboBox.Visibility = Visibility.Collapsed;
                IndVarComboBox2.Visibility = Visibility.Collapsed;
                SelectPhysicalLocText.Visibility = Visibility.Collapsed;
                PhysicalLocTextbox.Visibility = Visibility.Collapsed;
                SelectIndividualText.Visibility = Visibility.Collapsed;
                PhysicalLocComboBox.Visibility = Visibility.Collapsed;
                CreateLocButton.Visibility = Visibility.Collapsed;
                AssignLocButton.Visibility = Visibility.Collapsed;
                LocforRightText.Visibility = Visibility.Collapsed;
                LocforLeftText.Visibility = Visibility.Collapsed;

                if (g_CurrentArm == "Right")
                {
                    //create
                    g_CurrentArm = "Left";
                    YesLocButton.Visibility = Visibility.Visible;
                    NoLocButton.Visibility = Visibility.Visible;
                    isLeftLocText.Visibility = Visibility.Visible;
                }
                else
                {
                    //create
                    DoneLocalizingButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void DoneLocalizingButton_Click(object sender, RoutedEventArgs e)
        {
            DoneLocalizingButton.Visibility = Visibility.Collapsed;

            DoneAnnotatingButton.Visibility = Visibility.Visible;
        }

        private void DoneAnnotatingButton_Click(object sender, RoutedEventArgs e)
        {
            DoneAnnotatingButton.Visibility = Visibility.Collapsed;

            GestureIDTextbox.Visibility = Visibility.Visible;
            GestureIDButton.Visibility = Visibility.Visible;
            GestureIDTextInstruction1.Visibility = Visibility.Visible;
            GestureIDTextInstruction2.Visibility = Visibility.Visible;
            GestureIDTextInstruction3.Visibility = Visibility.Visible;
            GestureIDTextInstruction4.Visibility = Visibility.Visible;
            GestureIDTextInstruction5.Visibility = Visibility.Visible;
            QuitButton.Visibility = Visibility.Visible;

            saveGestureIDtoFile();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private async void loadGesturesIDsFile()
        {
            string filepath = @"JSONs\gesturesIDs.json";
            StorageFile file = await g_RootFolder.CreateFileAsync(filepath, CreationCollisionOption.OpenIfExists);
            var data = await FileIO.ReadTextAsync(file);

            g_AnnotatedGesturesID = new JArray();
            if (data.Length > 0)
            {
                g_AnnotatedGesturesID = JArray.Parse(data);
            }
        }

        private async void saveGestureIDtoFile()
        {
            //save gesture id to annotated id file
            string filepath = @"JSONs\gesturesIDs.json";
            StorageFile file = await g_RootFolder.CreateFileAsync(filepath, CreationCollisionOption.OpenIfExists);
            g_AnnotatedGesturesID.Add(g_SelectedGestureID);
            string string_to_send = JsonConvert.SerializeObject(g_AnnotatedGesturesID, Formatting.None);
            await FileIO.WriteTextAsync(file, string_to_send);
        }
    }
}
