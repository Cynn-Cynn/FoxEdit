
using FoxEdit.WindowComponents;
using FoxEdit.WindowPanels.SubPanels;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class AnimationsHandler : baseVoxelObjectEditorPanelHandler
    {
        private const string PlayRunningClassName = "animation-play-pause-button__running";

        private FrameSelectorElement frameSelector;
        private AnimationSelectorSubPanel animationSelector;
        private Button playPauseAnimButton;
        private Button stopAnimButton;

        public AnimationsHandler(VisualElement root) : base(root)
        {
        }

        public override void GetElements()
        {
            frameSelector = _root.Q<FrameSelectorElement>();
            animationSelector = new AnimationSelectorSubPanel(_root.Q("animation-selector-container"));
            playPauseAnimButton = _root.Q<Button>("animation-play-pause");
            stopAnimButton = _root.Q<Button>("animation-stop");
        }

        protected override void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            animationSelector.SetAnimationNames(voxelEditor.GetAnimationNames());
            animationSelector.SetAnimationIndex(0);

            voxelEditor.OnFramesThumbnailsUpdated += OnFrameThumnbailUpdated;
            voxelEditor.OnFrameIndexChanged += OnFrameIndexChanged;
            voxelEditor.OnAnimationIndexChanged += OnAnimationIndexChanged;

            UpdatePlayPauseButton();
            UpdateFrameSelector();
        }

        protected override void OnStopEditVoxelObject()
        {
            if (_voxelEditor != null)
            {
                _voxelEditor.OnFramesThumbnailsUpdated -= OnFrameThumnbailUpdated;
                _voxelEditor.OnFrameIndexChanged -= OnFrameIndexChanged;
                _voxelEditor.OnAnimationIndexChanged -= OnAnimationIndexChanged;
            }
        }

        private void OnAnimationIndexChanged(int newAnimationIndex)
        {
            animationSelector.SetAnimationIndex(newAnimationIndex);
            frameSelector.FramesCount = _voxelEditor.CurrentAnimation.FramesCount;
        }

        private void OnFrameIndexChanged(int frameIndex)
        {
            frameSelector.SelectFrame(frameIndex, false);
        }


        public override void RegisterCallbacks()
        {
            frameSelector.OnFrameChanged += OnSelectFrame;
            frameSelector.OnMoveFrame += OnMoveFrame;
            frameSelector.OnDuplicateFrame += OnDuplicateFrame;
            frameSelector.OnNewFrame += OnNewFrame;
            frameSelector.OnDeleteFrame += OnDeleteFrame;

            animationSelector.OnAddAnimation += OnAddAnimationFromAnimSelector;
            animationSelector.OnDeleteAnimation += OnDeleteAnimationFromAnimSelector;
            animationSelector.OnRenameAnimation += OnRenameAnimationFromAnimSelector;
            animationSelector.OnSelectAnimation += OnSelectAnimationFromAnimSelector;

            playPauseAnimButton.clicked += OnTogglePlayPause;
            stopAnimButton.clicked += OnStopAnimation;
        }

        public override void UnregisterCallbacks()
        {
            frameSelector.OnFrameChanged -= OnSelectFrame;
            frameSelector.OnMoveFrame -= OnMoveFrame;
            frameSelector.OnDuplicateFrame -= OnDuplicateFrame;
            frameSelector.OnNewFrame -= OnNewFrame;
            frameSelector.OnDeleteFrame -= OnDeleteFrame;

            animationSelector.OnAddAnimation -= OnAddAnimationFromAnimSelector;
            animationSelector.OnDeleteAnimation -= OnDeleteAnimationFromAnimSelector;
            animationSelector.OnRenameAnimation -= OnRenameAnimationFromAnimSelector;
            animationSelector.OnSelectAnimation -= OnSelectAnimationFromAnimSelector;

            playPauseAnimButton.clicked -= OnTogglePlayPause;
            stopAnimButton.clicked -= OnStopAnimation;
        }


        public override void SetupFields()
        {
        }

        private void UpdateFrameSelector()
        {
            frameSelector.FramesCount = _voxelEditor.CurrentAnimation.FramesCount;
            frameSelector.SelectFrame(0, false);
        }

        private void OnDeleteAnimationFromAnimSelector(int animIndex)
        {
            _voxelEditor.DeleteAnimation(animIndex);
            animationSelector.SetAnimationNames(_voxelEditor.GetAnimationNames());
        }

        private void OnDeleteFrame()
        {
            _voxelEditor.DeleteFrame();
        }

        private void OnNewFrame()
        {
            _voxelEditor.NewFrame();
        }

        private void OnDuplicateFrame()
        {
            _voxelEditor.DuplicateFrame();
        }

        private void OnMoveFrame(int oldIndex, int newIndex)
        {
            _voxelEditor.MoveFrame(oldIndex, newIndex);
        }

        private void OnFrameThumnbailUpdated(int animIndex, int index, Texture2D texture)
        {
            //frameSelector.SetFrameThumbnail(index, texture);
        }

        private void OnSelectFrame(int newFrame)
        {
            if (FoxEditManager.VoxelEditor != null)
                FoxEditManager.VoxelEditor.ChangeFrame(newFrame);
        }

        private void OnSelectAnimationFromAnimSelector(int animationIndex)
        {
            _voxelEditor.SelectedAnimationIndex = animationIndex;
        }

        private void OnRenameAnimationFromAnimSelector(int animIndex, string newName)
        {
            VoxelEditorAnimation voxelEditorAnimation = _voxelEditor.GetVoxelEditorAnimation(animIndex);
            voxelEditorAnimation.Name = newName;
        }

        private void OnAddAnimationFromAnimSelector(string obj)
        {
            _voxelEditor.NewAnimation(obj);
        }

        private void UpdatePlayPauseButton()
        {
            playPauseAnimButton.EnableInClassList(PlayRunningClassName, _voxelEditor.IsAnimationPreviewPlay);
            stopAnimButton.SetEnabled(_voxelEditor.CanStopAnimationPreview);
        }

        private void OnTogglePlayPause()
        {
            if (_voxelEditor.IsAnimationPreviewPlay)
                _voxelEditor.PauseAnimation();
            else
                _voxelEditor.PlayAnimation();

            UpdatePlayPauseButton();
        }

        private void OnStopAnimation()
        {
            _voxelEditor.StopAnimation();
            UpdatePlayPauseButton();
        }

    }
}