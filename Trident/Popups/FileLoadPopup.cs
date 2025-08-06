using ImGuiNET;

namespace Trident.Popups
{
    internal abstract class FileLoadPopup(string id) : IPopup
    {
        protected readonly string popupId = id;
        protected string selectedPath = "";
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
                    OnLoad(selectedPath);
                    ImGui.CloseCurrentPopup();
                    IsOpen = false;
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    IsOpen = false;
                }

                ImGui.EndPopup();
            }
        }

        protected abstract void OnLoad(string path);
    }
}