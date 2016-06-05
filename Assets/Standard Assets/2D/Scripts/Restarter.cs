using UnityEngine;
using UnityEngine.SceneManagement;

namespace Standard_Assets._2D.Scripts
{
    public class Restarter : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
            {
                SceneManager.LoadScene(SceneManager.GetSceneAt(0).path);
            }
        }
    }
}
