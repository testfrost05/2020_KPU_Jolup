using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{
    public class ItemSFX_Manager : MonoBehaviour //아이템 오디오 관련
    {
        public List<AudioClip> grabSounds = new List<AudioClip>(); //그렙 했을때 소리
        public List<AudioClip> dropSounds = new List<AudioClip>(); //떨궜을떄 소리

        public virtual void PlayRandomAudioClip(List<AudioClip> list, Vector3 position)
        {
            if (list.Count == 0)
            {
                return;
            }

            AudioClip randomAudioClipClone = list[Random.Range(0, list.Count - 1)];

            if (randomAudioClipClone)
            {
                AudioSource.PlayClipAtPoint(randomAudioClipClone, position); //해당 위치에서 재생
            }
        }

        public List<AudioSource> lastFX = new List<AudioSource>();

        public int fxIndex;

        public float soundFadeTime = 0.5f;
        public float soundDelayTime = 0.1f;


        
        public void GetAudioSources(int amount, GameObject audioSourceContainer)  //오디오소스 추가
        {
            AudioSource[] tempAudioSources = audioSourceContainer.GetComponents<AudioSource>();

            lastFX.AddRange(tempAudioSources);
        }

        public void RemoveAudioSources(GameObject audioSourceContainer) //오디오 소스 제거
        {
            AudioSource[] audioSources = audioSourceContainer.GetComponents<AudioSource>();

            for (int i = 0; i < audioSources.Length; i++)
            {
                if (lastFX.Contains(audioSources[i]))
                {
                    lastFX.Remove(audioSources[i]);

                    IEnumerator fadeRoutine = null;

                    if (fades.ContainsKey(audioSources[i]))
                    {
                        fades.TryGetValue(audioSources[i], out fadeRoutine);
                    }

                    if (fadeRoutine != null)
                    {
                        StopCoroutine(fadeRoutine);
                        fades.Remove(audioSources[i]);
                    }
                }
            }

            fxIndex = 0;
        }

        public void ClearAudioSources()
        {
            lastFX.Clear();
        }

        Dictionary<AudioSource, IEnumerator> fades = new Dictionary<AudioSource, IEnumerator>();

        protected void PlayRandomAudioClipFade(List<AudioClip> sounds, float delayTime, float fadeTime) //재생하면서 페이드 효과줌
        {
            if (sounds.Count == 0)
            {
                return;
            }

            int fadeIndex = fxIndex == 0 ? lastFX.Count - 1 : fxIndex - 1;

            IEnumerator tempFade = FadeOut(lastFX[fadeIndex], delayTime, fadeTime);

            fades.Add(lastFX[fadeIndex], tempFade);

            StartCoroutine(tempFade);

            AudioClip randomAudioClipClone = sounds[Random.Range(0, sounds.Count - 1)];

            AudioSource tempSource = lastFX[fxIndex];

            if (randomAudioClipClone)
            {
                IEnumerator fadeRoutine = null;

                if (fades.ContainsKey(tempSource))
                {
                    fades.TryGetValue(tempSource, out fadeRoutine);
                }

                if (fadeRoutine != null)
                {
                    StopCoroutine(fadeRoutine);
                    fades.Remove(tempSource);
                }

                tempSource.volume = 1;
                tempSource.clip = randomAudioClipClone;
                tempSource.Play();

                fxIndex = fxIndex == lastFX.Count - 1 ? 0 : fxIndex + 1;
            }
        }

        public IEnumerator FadeOut(AudioSource audioSource, float DelayTime, float FadeTime) //소리 서서히 사라지게
        {
            if (audioSource.clip == null)
            {
                yield break;
            }

            float startVolume = audioSource.volume;

            yield return new WaitForSeconds(DelayTime);

            while (audioSource)
            {
                if (audioSource.volume > 0)
                {
                    audioSource.volume -= startVolume * Time.deltaTime / FadeTime;
                }
                else
                {
                    break;
                }

                yield return null;
            }

            if (audioSource)
            {
                audioSource.Stop();
                audioSource.volume = startVolume;
                audioSource.clip = null;

                fades.Remove(audioSource);
            }
        }
    }
}