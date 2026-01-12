using UnityEngine;

namespace Geuneda.UiService
{
    /// <summary>
    /// This class is responsible for updating the resolution and device orientation of the <see cref="UiService"/>
    /// </summary>
    public class UiServiceMonoComponent : MonoBehaviour
    {
        private Vector2 _resolution;
        private DeviceOrientation _orientation;
        
        private void Awake()
        {
            _resolution = new Vector2(Screen.width, Screen.height);
            _orientation = Input.deviceOrientation;
        }
        
        private void Update()
        {
            if (!Mathf.Approximately(_resolution.x, Screen.width) || !Mathf.Approximately(_resolution.y, Screen.height)) 
            {
                var previousResolution = _resolution;
                
                _resolution = new Vector2(Screen.width, Screen.height);
                
                UiService.OnResolutionChanged.Invoke(previousResolution, _resolution);
            }
            
            switch (Input.deviceOrientation) {
                case DeviceOrientation.Unknown:            // Ignore
                case DeviceOrientation.FaceUp:            // Ignore
                case DeviceOrientation.FaceDown:        // Ignore
                    break;
                default:
                    if (_orientation != Input.deviceOrientation) 
                    {
                        var previousOrientation = _orientation;
                        
                        _orientation = Input.deviceOrientation;
                        
                        UiService.OnOrientationChanged.Invoke(previousOrientation, _orientation);
                    }
                    break;
            }
        }
    }
}