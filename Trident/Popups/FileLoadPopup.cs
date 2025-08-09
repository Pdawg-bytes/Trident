using ImGuiNET;
using System.Numerics;
using Trident.Utilities;

namespace Trident.Popups
{
    internal abstract class FileLoadPopup(string id) : IPopup
    {
        protected readonly string popupId = id;
        protected string selectedPath = "";

        private string _errorMessage = string.Empty;
        private static readonly Vector4 _errorColor = new(.98f, .165f, .33f, 1f);

        public bool IsOpen { get; private set; }


        public void Open()
        {
            ImGui.OpenPopup(popupId);
            IsOpen = true;
        }

        public void Render()
        {
            if (ImGui.BeginPopupModal(popupId, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("File Path", ref selectedPath, 256);

                if (ImGui.Button("Load"))
                {
                    selectedPath = PathSanitizer.SanitizePath(selectedPath);
                    if (Path.Exists(selectedPath))
                    {
                        OnLoad(selectedPath);
                        ImGui.CloseCurrentPopup();
                        IsOpen = false;
                        _errorMessage = string.Empty;
                    }
                    else
                        _errorMessage = "The path is invalid or the file does not exist.";
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    IsOpen = false;
                    _errorMessage = string.Empty;
                }

                if (!string.IsNullOrEmpty(_errorMessage))
                {
                    ImGui.Spacing();
                    ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
                    ImGui.TextColored(_errorColor, _errorMessage);
                    ImGui.PopTextWrapPos();
                }

                ImGui.EndPopup();
            }
        }

        protected abstract void OnLoad(string path);
    }
}