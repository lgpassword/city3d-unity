using UnityEngine;

/// <summary>
/// 鼠标控制的轨道相机。
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    // 相机到目标点的距离。
    public float distance = 150f;

    // 鼠标旋转灵敏度。
    public float sensitivity = 60f;

    // 滚轮缩放速度。
    public float zoomSpeed = 12f;

    // 右键平移速度。
    public float panSpeed = 0.3f;

    // 垂直角度和水平角度。
    private float _theta = 30f, _phi = 45f;

    // 相机观察目标点。
    private Vector3 _target = new(0, 10, 0);

    // 在所有对象更新后刷新相机位置。
    private void LateUpdate()
    {
        // 左键拖动旋转视角。
        if (Input.GetMouseButton(0))
        {
            _phi += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            _theta -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
            _theta = Mathf.Clamp(_theta, 3f, 87f);
        }

        // 右键拖动平移观察目标。
        if (Input.GetMouseButton(1))
        {
            var r = transform.right * -Input.GetAxis("Mouse X");
            var u = transform.up * Input.GetAxis("Mouse Y");
            _target += (r + u) * panSpeed * (distance * .01f);
        }

        // 滚轮缩放相机距离。
        distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * zoomSpeed, 8f, 600f);

        float radT = _theta * Mathf.Deg2Rad, radP = _phi * Mathf.Deg2Rad;
        transform.position = _target + new Vector3(
            distance * Mathf.Cos(radT) * Mathf.Sin(radP),
            distance * Mathf.Sin(radT),
            distance * Mathf.Cos(radT) * Mathf.Cos(radP));
        transform.LookAt(_target);
    }
}
