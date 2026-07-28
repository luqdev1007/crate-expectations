using UnityEngine;

namespace CrateExpectations.Player
{
    /// <summary>
    /// Держит курсор запертым в окне, пока играем от первого лица.
    /// Отдельный компонент, чтобы контроллер занимался только движением
    /// </summary>
    public sealed class CursorLocker : MonoBehaviour
    {
        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
