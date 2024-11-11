using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NiVE3.Data.Config;
using NiVE3.Extension;
using NiVE3.Util;
using NiVE3.Wpf.Input;
using NiVE3.SourceGenerator.ResourceMarkupGenerator;
using NiVE3.Wpf.Behavior;
using System.Text.RegularExpressions;

namespace NiVE3.Config
{
    [MarkupableBinding(InstancePropertyName = nameof(Setting), OverrideExtensionName = "ShortcutKeyBinding", RoutingCommandOwnerType = typeof(WindowGestureBehavior), RoutingCommandName = "Gesture")]
    partial class ShortcutKeySetting : DependencyObject
    {
        [GeneratedRegex("Property$")]
        private static partial Regex DependencyPropertyNameRegex();

        static readonly string FilePath = Path.Combine(Paths.ConfigDirectory, "shortcut_keys.json");

        public static ShortcutKeySetting Setting { get; }

        public static Dictionary<string, DependencyProperty> DependencyProperties { get; }

        public static Dictionary<ShortcutKeyCategoryType, string[]> CategorizedShortcutKeys { get; }

        public static readonly DependencyProperty NewProjectGestureProperty = DependencyProperty.Register(
            nameof(NewProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.N, ModifierKeys.Alt))
        );

        public static readonly DependencyProperty OpenProjectGestureProperty = DependencyProperty.Register(
            nameof(OpenProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.O, ModifierKeys.Control))
        );

        public static readonly DependencyProperty SaveProjectGestureProperty = DependencyProperty.Register(
            nameof(SaveProjectGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.S, ModifierKeys.Control))
        );

        public static readonly DependencyProperty SaveProjectAsNewNameGestureProperty = DependencyProperty.Register(
            nameof(SaveProjectAsNewNameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift))
        );

        public static readonly DependencyProperty ExitGestureProperty = DependencyProperty.Register(
            nameof(ExitGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.F4, ModifierKeys.Alt))
        );

        public static readonly DependencyProperty DeleteItemGestureProperty = DependencyProperty.Register(
            nameof(DeleteItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.Delete))
        );

        public static readonly DependencyProperty CutItemGestureProperty = DependencyProperty.Register(
            nameof(CutItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.X, ModifierKeys.Control))
        );

        public static readonly DependencyProperty CopyItemGestureProperty = DependencyProperty.Register(
            nameof(CopyItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.C, ModifierKeys.Control))
        );

        public static readonly DependencyProperty PasteItemGestureProperty = DependencyProperty.Register(
            nameof(PasteItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.V, ModifierKeys.Control))
        );

        public static readonly DependencyProperty DuplicateItemGestureProperty = DependencyProperty.Register(
            nameof(DuplicateItemGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.D, ModifierKeys.Control))
        );

        public static readonly DependencyProperty SplitLayerGestureProperty = DependencyProperty.Register(
            nameof(SplitLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.D, ModifierKeys.Shift | ModifierKeys.Control))
        );

        public static readonly DependencyProperty NewCompositionGestureProperty = DependencyProperty.Register(
            nameof(NewCompositionGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.N, ModifierKeys.Control))
        );

        public static readonly DependencyProperty AddSolidLayerGestureProperty = DependencyProperty.Register(
            nameof(AddSolidLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Y, ModifierKeys.Control))
        );

        public static readonly DependencyProperty NewFootageFolderGestureProperty = DependencyProperty.Register(
            nameof(NewFootageFolderGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.N, ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt))
        );

        public static readonly DependencyProperty UndoGestureProperty = DependencyProperty.Register(
            nameof(UndoGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Z, ModifierKeys.Control))
        );

        public static readonly DependencyProperty RedoGestureProperty = DependencyProperty.Register(
            nameof(RedoGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Z, ModifierKeys.Shift | ModifierKeys.Control))
        );

        public static readonly DependencyProperty BeginEditNameGestureProperty = DependencyProperty.Register(
            nameof(BeginEditNameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.Enter))
        );

        public static readonly DependencyProperty OpenRenderSettingGestureProperty = DependencyProperty.Register(
            nameof(OpenRenderSettingGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.M, ModifierKeys.Control))
        );

        public static readonly DependencyProperty SelectHandToolGestureProperty = DependencyProperty.Register(
            nameof(SelectHandToolGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.H))
        );

        public static readonly DependencyProperty SelectSelectToolGestureProperty = DependencyProperty.Register(
            nameof(SelectSelectToolGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.V))
        );

        public static readonly DependencyProperty SelectRotateToolGestureProperty = DependencyProperty.Register(
            nameof(SelectRotateToolGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.W))
        );

        public static readonly DependencyProperty SelectScaleGestureGestureProperty = DependencyProperty.Register(
            nameof(SelectScaleGestureGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.Y))
        );

        public static readonly DependencyProperty SelectCameraToolGestureProperty = DependencyProperty.Register(
            nameof(SelectCameraToolGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.C))
        );

        public static readonly DependencyProperty SelectAllGestureProperty = DependencyProperty.Register(
            nameof(SelectAllGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.A, ModifierKeys.Control))
        );

        public static readonly DependencyProperty AddShapeLayerGestureProperty = DependencyProperty.Register(
            nameof(AddShapeLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty AddCameraLayerGestureProperty = DependencyProperty.Register(
            nameof(AddCameraLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty AddLightLayerGestureProperty = DependencyProperty.Register(
            nameof(AddLightLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty AddNullObjectLayerGestureProperty = DependencyProperty.Register(
            nameof(AddNullObjectLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty AddTextLayerGestureProperty = DependencyProperty.Register(
            nameof(AddTextLayerGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty OpenCompositionSettingGestureProperty = DependencyProperty.Register(
            nameof(OpenCompositionSettingGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty LoadSolidGestureProperty = DependencyProperty.Register(
            nameof(LoadSolidGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty LoadFileGestureProperty = DependencyProperty.Register(
            nameof(LoadFileGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty AddKeyFrameGestureProperty = DependencyProperty.Register(
            nameof(AddKeyFrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty ResetPropertyGestureProperty = DependencyProperty.Register(
            nameof(ResetPropertyGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty OpenCommandPaletteGestureProperty = DependencyProperty.Register(
            nameof(OpenCommandPaletteGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Space, ModifierKeys.Control))
        );

        public static readonly DependencyProperty ChangeLayerTagsRandomlyGestureProperty = DependencyProperty.Register(
            nameof(ChangeLayerTagsRandomlyGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        public static readonly DependencyProperty MoveInPointToIndicatorGestureProperty = DependencyProperty.Register(
            nameof(MoveInPointToIndicatorGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.OemOpenBrackets))
        );

        public static readonly DependencyProperty MoveOutPointToIndicatorGestureProperty = DependencyProperty.Register(
            nameof(MoveOutPointToIndicatorGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.OemCloseBrackets))
        );

        public static readonly DependencyProperty ShiftSourceStartPointToNextFrameGestureProperty = DependencyProperty.Register(
            nameof(ShiftSourceStartPointToNextFrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.PageDown, ModifierKeys.Alt))
        );

        public static readonly DependencyProperty ShiftSourceStartPointToPreviousFrameGestureProperty = DependencyProperty.Register(
            nameof(ShiftSourceStartPointToPreviousFrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.PageUp, ModifierKeys.Alt))
        );

        public static readonly DependencyProperty ShiftSourceStartPointToNext10FrameGestureProperty = DependencyProperty.Register(
            nameof(ShiftSourceStartPointToNext10FrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.PageDown, ModifierKeys.Shift | ModifierKeys.Alt))
        );

        public static readonly DependencyProperty ShiftSourceStartPointToPrevious10FrameGestureProperty = DependencyProperty.Register(
            nameof(ShiftSourceStartPointToPrevious10FrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.PageUp, ModifierKeys.Shift | ModifierKeys.Alt))
        );

        public static readonly DependencyProperty MoveIndicatorToNextFrameGestureProperty = DependencyProperty.Register(
            nameof(MoveIndicatorToNextFrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Right, ModifierKeys.Control))
        );

        public static readonly DependencyProperty MoveIndicatorToPreviousFrameGestureProperty = DependencyProperty.Register(
            nameof(MoveIndicatorToPreviousFrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Left, ModifierKeys.Control))
        );

        public static readonly DependencyProperty MoveIndicatorToNext10FrameGestureProperty = DependencyProperty.Register(
            nameof(MoveIndicatorToNext10FrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Right, ModifierKeys.Shift | ModifierKeys.Control))
        );

        public static readonly DependencyProperty MoveIndicatorToPrevious10FrameGestureProperty = DependencyProperty.Register(
            nameof(MoveIndicatorToPrevious10FrameGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.Left, ModifierKeys.Shift | ModifierKeys.Control))
        );

        public static readonly DependencyProperty MoveIndicatorToCompositionBeginGestureProperty = DependencyProperty.Register(
            nameof(MoveIndicatorToCompositionBeginGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.Home))
        );

        public static readonly DependencyProperty MoveIndicatorToCompositionEndGestureProperty = DependencyProperty.Register(
            nameof(MoveIndicatorToCompositionEndGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new SingleKeyGesture(Key.End))
        );

        public static readonly DependencyProperty ChangeLayerPlayRateGestureProperty = DependencyProperty.Register(
            nameof(ChangeLayerPlayRateGesture),
            typeof(InputGesture),
            typeof(ShortcutKeySetting),
            new PropertyMetadata(new KeyGesture(Key.None))
        );

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ChangeLayerPlayRateGesture
        {
            get { return (InputGesture)GetValue(ChangeLayerPlayRateGestureProperty); }
            set { SetValue(ChangeLayerPlayRateGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture MoveIndicatorToCompositionEndGesture
        {
            get { return (InputGesture)GetValue(MoveIndicatorToCompositionEndGestureProperty); }
            set { SetValue(MoveIndicatorToCompositionEndGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture MoveIndicatorToCompositionBeginGesture
        {
            get { return (InputGesture)GetValue(MoveIndicatorToCompositionBeginGestureProperty); }
            set { SetValue(MoveIndicatorToCompositionBeginGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture MoveIndicatorToPrevious10FrameGesture
        {
            get { return (InputGesture)GetValue(MoveIndicatorToPrevious10FrameGestureProperty); }
            set { SetValue(MoveIndicatorToPrevious10FrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture MoveIndicatorToNext10FrameGesture
        {
            get { return (InputGesture)GetValue(MoveIndicatorToNext10FrameGestureProperty); }
            set { SetValue(MoveIndicatorToNext10FrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture MoveIndicatorToPreviousFrameGesture
        {
            get { return (InputGesture)GetValue(MoveIndicatorToPreviousFrameGestureProperty); }
            set { SetValue(MoveIndicatorToPreviousFrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture MoveIndicatorToNextFrameGesture
        {
            get { return (InputGesture)GetValue(MoveIndicatorToNextFrameGestureProperty); }
            set { SetValue(MoveIndicatorToNextFrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ShiftSourceStartPointToPrevious10FrameGesture
        {
            get { return (InputGesture)GetValue(ShiftSourceStartPointToPrevious10FrameGestureProperty); }
            set { SetValue(ShiftSourceStartPointToPrevious10FrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ShiftSourceStartPointToNext10FrameGesture
        {
            get { return (InputGesture)GetValue(ShiftSourceStartPointToNext10FrameGestureProperty); }
            set { SetValue(ShiftSourceStartPointToNext10FrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ShiftSourceStartPointToPreviousFrameGesture
        {
            get { return (InputGesture)GetValue(ShiftSourceStartPointToPreviousFrameGestureProperty); }
            set { SetValue(ShiftSourceStartPointToPreviousFrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ShiftSourceStartPointToNextFrameGesture
        {
            get { return (InputGesture)GetValue(ShiftSourceStartPointToNextFrameGestureProperty); }
            set { SetValue(ShiftSourceStartPointToNextFrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture MoveOutPointToIndicatorGesture
        {
            get { return (InputGesture)GetValue(MoveOutPointToIndicatorGestureProperty); }
            set { SetValue(MoveOutPointToIndicatorGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture MoveInPointToIndicatorGesture
        {
            get { return (InputGesture)GetValue(MoveInPointToIndicatorGestureProperty); }
            set { SetValue(MoveInPointToIndicatorGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ChangeLayerTagsRandomlyGesture
        {
            get { return (InputGesture)GetValue(ChangeLayerTagsRandomlyGestureProperty); }
            set { SetValue(ChangeLayerTagsRandomlyGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Other)]
        [ShowInMarkup]
        public InputGesture OpenCommandPaletteGesture
        {
            get { return (InputGesture)GetValue(OpenCommandPaletteGestureProperty); }
            set { SetValue(OpenCommandPaletteGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture ResetPropertyGesture
        {
            get { return (InputGesture)GetValue(ResetPropertyGestureProperty); }
            set { SetValue(ResetPropertyGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture AddKeyFrameGesture
        {
            get { return (InputGesture)GetValue(AddKeyFrameGestureProperty); }
            set { SetValue(AddKeyFrameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Footage)]
        [ShowInMarkup]
        public InputGesture LoadFileGesture
        {
            get { return (InputGesture)GetValue(LoadFileGestureProperty); }
            set { SetValue(LoadFileGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Footage)]
        [ShowInMarkup]
        public InputGesture LoadSolidGesture
        {
            get { return (InputGesture)GetValue(LoadSolidGestureProperty); }
            set { SetValue(LoadSolidGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture OpenCompositionSettingGesture
        {
            get { return (InputGesture)GetValue(OpenCompositionSettingGestureProperty); }
            set { SetValue(OpenCompositionSettingGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture AddTextLayerGesture
        {
            get { return (InputGesture)GetValue(AddTextLayerGestureProperty); }
            set { SetValue(AddTextLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture AddNullObjectLayerGesture
        {
            get { return (InputGesture)GetValue(AddNullObjectLayerGestureProperty); }
            set { SetValue(AddNullObjectLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture AddLightLayerGesture
        {
            get { return (InputGesture)GetValue(AddLightLayerGestureProperty); }
            set { SetValue(AddLightLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture AddCameraLayerGesture
        {
            get { return (InputGesture)GetValue(AddCameraLayerGestureProperty); }
            set { SetValue(AddCameraLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture AddShapeLayerGesture
        {
            get { return (InputGesture)GetValue(AddShapeLayerGestureProperty); }
            set { SetValue(AddShapeLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture SelectAllGesture
        {
            get { return (InputGesture)GetValue(SelectAllGestureProperty); }
            set { SetValue(SelectAllGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture SelectCameraToolGesture
        {
            get { return (InputGesture)GetValue(SelectCameraToolGestureProperty); }
            set { SetValue(SelectCameraToolGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Tool)]
        [ShowInMarkup]
        public InputGesture SelectScaleGestureGesture
        {
            get { return (InputGesture)GetValue(SelectScaleGestureGestureProperty); }
            set { SetValue(SelectScaleGestureGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Tool)]
        [ShowInMarkup]
        public InputGesture SelectRotateToolGesture
        {
            get { return (InputGesture)GetValue(SelectRotateToolGestureProperty); }
            set { SetValue(SelectRotateToolGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Tool)]
        [ShowInMarkup]
        public InputGesture SelectSelectToolGesture
        {
            get { return (InputGesture)GetValue(SelectSelectToolGestureProperty); }
            set { SetValue(SelectSelectToolGestureProperty, value); }
        }
        
        [ShortcutKeyCategory(ShortcutKeyCategoryType.Tool)]
        [ShowInMarkup]
        public InputGesture SelectHandToolGesture
        {
            get { return (InputGesture)GetValue(SelectHandToolGestureProperty); }
            set { SetValue(SelectHandToolGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture OpenRenderSettingGesture
        {
            get { return (InputGesture)GetValue(OpenRenderSettingGestureProperty); }
            set { SetValue(OpenRenderSettingGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture BeginEditNameGesture
        {
            get { return (InputGesture)GetValue(BeginEditNameGestureProperty); }
            set { SetValue(BeginEditNameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture RedoGesture
        {
            get { return (InputGesture)GetValue(RedoGestureProperty); }
            set { SetValue(RedoGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture UndoGesture
        {
            get { return (InputGesture)GetValue(UndoGestureProperty); }
            set { SetValue(UndoGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Footage)]
        [ShowInMarkup]
        public InputGesture NewFootageFolderGesture
        {
            get { return (InputGesture)GetValue(NewFootageFolderGestureProperty); }
            set { SetValue(NewFootageFolderGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Composition)]
        [ShowInMarkup]
        public InputGesture AddSolidLayerGesture
        {
            get { return (InputGesture)GetValue(AddSolidLayerGestureProperty); }
            set { SetValue(AddSolidLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.File)]
        [ShowInMarkup]
        public InputGesture SaveProjectAsNewNameGesture
        {
            get { return (InputGesture)GetValue(SaveProjectAsNewNameGestureProperty); }
            set { SetValue(SaveProjectAsNewNameGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.File)]
        [ShowInMarkup]
        public InputGesture SaveProjectGesture
        {
            get { return (InputGesture)GetValue(SaveProjectGestureProperty); }
            set { SetValue(SaveProjectGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.File)]
        [ShowInMarkup]
        public InputGesture OpenProjectGesture
        {
            get { return (InputGesture)GetValue(OpenProjectGestureProperty); }
            set { SetValue(OpenProjectGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.File)]
        [ShowInMarkup]
        public InputGesture NewProjectGesture
        {
            get { return (InputGesture)GetValue(NewProjectGestureProperty); }
            set { SetValue(NewProjectGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.File)]
        [ShowInMarkup]
        public InputGesture ExitGesture
        {
            get { return (InputGesture)GetValue(ExitGestureProperty); }
            set { SetValue(ExitGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture SplitLayerGesture
        {
            get { return (InputGesture)GetValue(SplitLayerGestureProperty); }
            set { SetValue(SplitLayerGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture DuplicateItemGesture
        {
            get { return (InputGesture)GetValue(DuplicateItemGestureProperty); }
            set { SetValue(DuplicateItemGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture PasteItemGesture
        {
            get { return (InputGesture)GetValue(PasteItemGestureProperty); }
            set { SetValue(PasteItemGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture CopyItemGesture
        {
            get { return (InputGesture)GetValue(CopyItemGestureProperty); }
            set { SetValue(CopyItemGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture CutItemGesture
        {
            get { return (InputGesture)GetValue(CutItemGestureProperty); }
            set { SetValue(CutItemGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.Edit)]
        [ShowInMarkup]
        public InputGesture DeleteItemGesture
        {
            get { return (InputGesture)GetValue(DeleteItemGestureProperty); }
            set { SetValue(DeleteItemGestureProperty, value); }
        }

        [ShortcutKeyCategory(ShortcutKeyCategoryType.File)]
        [ShowInMarkup]
        public InputGesture NewCompositionGesture
        {
            get { return (InputGesture)GetValue(NewCompositionGestureProperty); }
            set { SetValue(NewCompositionGestureProperty, value); }
        }

        static ShortcutKeySetting()
        {
            Setting = new ShortcutKeySetting();

            var fields = typeof(ShortcutKeySetting).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(DependencyProperty)).ToArray();
            var regex = DependencyPropertyNameRegex();
            DependencyProperties = fields.Select(f => Tuple.Create(f.Name, (DependencyProperty)f.GetValue(null)!))
                .ToDictionary(t => regex.Replace(t.Item1, ""), t => t.Item2);

            var fieldNames = fields.Select(p => p.Name.Replace("Property", "")).ToArray();
            var gestureProperties = typeof(ShortcutKeySetting).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => fieldNames.Contains(p.Name));
            CategorizedShortcutKeys = gestureProperties.GroupBy(p => p.GetCustomAttribute<ShortcutKeyCategoryAttribute>()?.Type ?? ShortcutKeyCategoryType.Other, p => p.Name)
                .ToDictionary(g => g.Key, g => g.ToArray());

            Setting.Load();
        }

        private ShortcutKeySetting() { }

        public void Save()
        {
            var data = DependencyProperties.Values.Select(dp =>
            {
                return Setting.GetValue(dp) switch
                {
                    KeyGesture keyGesture => new ShortcutKeyData
                    {
                        Name = dp.Name,
                        Type = nameof(KeyGesture),
                        GestureKey = keyGesture.Key,
                        Modifier = keyGesture.Modifiers
                    },
                    SingleKeyGesture singleKeyGesture => new ShortcutKeyData
                    {
                        Name = dp.Name,
                        Type = nameof(SingleKeyGesture),
                        GestureKey = singleKeyGesture.Key,
                        Modifier = singleKeyGesture.IsUseShift ? ModifierKeys.Shift : ModifierKeys.None
                    },
                    _ => new ShortcutKeyData
                    {
                        Name = dp.Name,
                        Type = nameof(KeyGesture),
                        GestureKey = Key.None,
                        Modifier = ModifierKeys.None
                    },
                };
            }).ToArray();

            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(FilePath, json);
        }

        void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            try
            {
                var shortcutKeyData = JsonSerializer.Deserialize<ShortcutKeyData[]>(File.ReadAllBytes(FilePath)) ?? [];
                foreach (var data in shortcutKeyData)
                {
                    data.CorrectData();
                    if (!DependencyProperties.ContainsKey(data.Name))
                    {
                        continue;
                    }

                    switch (data.Type)
                    {
                        case nameof(KeyGesture):
                            SetValue(DependencyProperties[data.Name], new KeyGesture(data.GestureKey, data.Modifier));
                            break;
                        case nameof(SingleKeyGesture):
                            SetValue(DependencyProperties[data.Name], new SingleKeyGesture(data.GestureKey, data.Modifier == ModifierKeys.Shift));
                            break;
                    }
                }

                var keys = DependencyProperties.Keys.ToArray();
                var values = DependencyProperties.Values.Select(GetValue).OfType<InputGesture>().ToArray();
                for (var i = 0; i < keys.Length; i++)
                {
                    if (values[i] is KeyGesture ka && ka.Key == Key.None)
                    {
                        continue;
                    }

                    for (var n = i + 1; n < keys.Length; n++)
                    {
                        if (values[n] is KeyGesture kb && kb.Key == Key.None)
                        {
                            continue;
                        }

                        if (values[i].IsSameKeyGesture(values[n]))
                        {
                            values[n] = new KeyGesture(Key.None);
                            SetValue(DependencyProperties[keys[n]], values[n]);
                        }
                    }
                }
            }
            catch { }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class ShortcutKeyCategoryAttribute : Attribute
    {
        public ShortcutKeyCategoryType Type { get; }

        public ShortcutKeyCategoryAttribute(ShortcutKeyCategoryType type)
        {
            Type = type;
        }
    }

    enum ShortcutKeyCategoryType
    {
        File,
        Edit,
        Footage,
        Composition,
        Layer,
        Tool,
        Other
    }
}
