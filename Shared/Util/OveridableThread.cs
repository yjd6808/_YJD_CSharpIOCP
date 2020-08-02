// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-01 오후 6:09:09   
// @PURPOSE     : 기존 쓰레드는 오버라이딩 기능이 없어서 추가해서 만듬
// ===============================


using System;
using System.Threading;

namespace Shared.Util
{
    public class OveridableThread
    {
        protected Thread RunningThread { get; private set; }

        private Action _ThreadAction;
        private Action<object> _ThreadParameterizedAction;
        private object _ThreadParameter;

        public OveridableThread()
        {
            RunningThread = null;

            _ThreadAction = null;
            _ThreadParameter = null;
            _ThreadParameterizedAction = null;
        }

        public OveridableThread(Action action)
        {
            RunningThread = null;

            _ThreadAction = action;
            _ThreadParameter = null;
            _ThreadParameterizedAction = null;
        }

        public OveridableThread(Action<object> parameterizedAction, object param)
        {
            RunningThread = null;

            _ThreadAction = null;
            _ThreadParameter = param;
            _ThreadParameterizedAction = parameterizedAction;
        }

        public virtual void StartThread()
        {
            //이미 시작된 경우는 경고메시지를 뛰워주자
            if (RunningThread != null && 
               (RunningThread.ThreadState & ThreadState.Unstarted) != ThreadState.Unstarted)
            {
                System.Diagnostics.Debug.Assert(false, "이미 시작되었던 쓰레드입니다.");
                return;
            }

            RunningThread = new Thread(OveridableThread.EntryPoint);
            RunningThread.Start(this);
        }

        protected virtual void Execute()
        {
            if (_ThreadAction != null)
                _ThreadAction();
            else if (_ThreadParameterizedAction != null && _ThreadParameter != null)
                _ThreadParameterizedAction(_ThreadParameter);
        }


        /// <summary>
        /// 쓰레드 시작지점
        /// </summary>
        /// <param name="param">자기자신</param>
        private static void EntryPoint(object param)
        {
            ((OveridableThread)param).Execute();
        }
    }
}
