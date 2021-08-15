using System.Security.AccessControl;
using System.Speech.Synthesis;
using System;

namespace Jisarv.SpeechEngine{
    class SpeechOutput{
        public SpeechOutput(){

        }

        public void Speak(string text){

            var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.SelectVoice("Microsoft Zira Desktop");
            synthesizer.Speak(text);
        }
    }
}