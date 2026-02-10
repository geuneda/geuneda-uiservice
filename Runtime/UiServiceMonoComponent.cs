using UnityEngine;

namespace Geuneda.UiService
{
    /// <summary>
    /// 이 클래스는 <see cref="UiService"/>의 해상도와 기기 방향을 업데이트하는 역할을 합니다
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
                case DeviceOrientation.Unknown:            // 무시
                case DeviceOrientation.FaceUp:            // 무시
                case DeviceOrientation.FaceDown:        // 무시
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