using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public sealed class SpringSpirit : MonoBehaviour
{
    public enum Season { Spring, Summer, Autumn, Winter }
    public Season season;
    public string referenceName = "spring_zhiyi";
    public string characterName = "春知意";
    public string excerpt = "把第一缕春风捎给赶路的人。";
    public SpriteRenderer portrait;
    public Sprite petal;
    Transform[] petals;
    TrackingVisibility visibility;
    float appeared;
    float burst = -10f;

    void Awake()
    {
        petals = new Transform[season == Season.Summer ? 4 : 16];
        for (int i = 0; i < petals.Length; i++)
        {
            var leaf = new GameObject("季节特效", typeof(SpriteRenderer));
            leaf.transform.SetParent(transform, false);
            leaf.GetComponent<SpriteRenderer>().sprite = petal;
            petals[i] = leaf.transform;
        }
    }

    void OnEnable() => appeared = Time.unscaledTime;
    FlowerCompanion companion;
    public Vector3 Shoulder => (companion ? companion.transform : transform).TransformPoint(Vector3.up*.078f);

    public void ShowCompanion(Camera camera)
    {
        if(companion)return;
        var rig = new GameObject("卡上花灵");
        rig.transform.SetParent(transform,false);
        Vector3 up=transform.up;
        if(Mathf.Abs(Vector3.Dot(up,Vector3.up)) < .55f)
            up=Vector3.ProjectOnPlane(Vector3.up,up).normalized;
        Vector3 towards=Vector3.ProjectOnPlane(camera.transform.position-transform.position,up);
        if(towards.sqrMagnitude>.00001f) rig.transform.rotation=Quaternion.LookRotation(towards,up);
        companion=rig.AddComponent<FlowerCompanion>();
        companion.Initialize(referenceName,.105f);
        portrait.enabled=false;
        portrait.GetComponent<BoxCollider>().enabled=false;
    }

    public void Blossom()
    {
        burst = Time.unscaledTime;
        if(companion) companion.Respond();
    }

    // 只在首次 Limited 时启动倒计时，连续 Limited 事件不会延长缓冲。
    public void Track(TrackingState state, float grace)
    {
        float now = Time.unscaledTime;
        visibility.Update(state, now, grace);
        gameObject.SetActive(visibility.IsVisible(now));
    }

    void LateUpdate()
    {
        float now = Time.unscaledTime;
        if (!visibility.IsVisible(now)) { gameObject.SetActive(false); return; }
        if (!Camera.main) return;
        float age = now - appeared;
        float fade = Mathf.Clamp01(age / .35f);
        float pulse = Mathf.Max(0, 1 - (now - burst) / .8f);
        portrait.color = new Color(1, 1, 1, fade);
        float bounce = Mathf.Sin((1-pulse)*Mathf.PI*2)*pulse;
        portrait.transform.localScale = Vector3.one * (.84f + .16f * fade + .18f * bounce);
        portrait.transform.localPosition = Vector3.up * (.006f + .003f * Mathf.Sin(age * 2) + .009f * Mathf.Max(0,bounce));
        portrait.transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(0,0,4*bounce);
        for (int i = 0; i < petals.Length; i++)
        {
            float t = Mathf.Repeat(age * .22f + i * .618f, 1);
            if (season == Season.Summer)
            {
                float ring = Mathf.Repeat(age * .30f + i * .25f, 1);
                petals[i].localPosition = Vector3.up * (.001f + i * .0003f);
                petals[i].localRotation = Quaternion.Euler(90, 0, 0);
                petals[i].localScale = Vector3.one * (ring * 6 + pulse * 2);
                petals[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, Mathf.Sin(ring * Mathf.PI));
                continue;
            }
            float a = i * 2.4f + age * .7f;
            float r = .026f + .017f * Mathf.Sin(i * 7) + .033f * pulse;
            petals[i].localPosition = new Vector3(Mathf.Cos(a) * r, .105f * (1 - t), Mathf.Sin(a) * r);
            if(companion && now-burst>=.26f && now-burst<1.05f)
            {
                float flight=(now-burst-.26f)/.79f;
                Vector3 spread=companion.transform.right*Mathf.Cos(i*2.4f)+companion.transform.forward*Mathf.Sin(i*2.4f);
                if(season==Season.Autumn)
                {
                    float arc=i*2.4f+flight*5.2f;
                    spread=companion.transform.right*Mathf.Cos(arc)+companion.transform.forward*Mathf.Sin(arc);
                }
                petals[i].position=companion.Palm+spread*(flight*.042f)+companion.transform.up*(.045f*Mathf.Sin(flight*Mathf.PI));
            }
            petals[i].rotation = Camera.main.transform.rotation * Quaternion.Euler(0, 0, i * 43 + age * 65);
            petals[i].localScale = Vector3.one * Mathf.Sin(Mathf.PI * t);
        }
    }
}
