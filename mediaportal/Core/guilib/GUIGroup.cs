using System;
using System.Collections;

namespace MediaPortal.GUI.Library
{
	/// <summary>
	/// GUIWindow.RestoreControlPositions
	/// </summary>
	public class GUIGroup: GUIControl
	{
    protected ArrayList     m_Controls = new ArrayList();
    [XMLSkinElement("animation")] Animator.AnimationType m_Animation=Animator.AnimationType.None;
    protected bool          m_bStart=false;
    protected Animator      m_animator;

		public GUIGroup()
		{
		}
		public GUIGroup(int dwParentID) : base(dwParentID)
		{
		}

    public Animator.AnimationType Animation
    {
      get { return m_Animation;}
      set { 
        m_Animation=value;
      }
    }

    public override void OnInit()
    {
      m_bStart   = true;
      m_animator = new Animator(m_Animation);
    }

    public void AddControl(GUIControl control)
    {
        m_Controls.Add(control);
    }

    public int Count
    {
        get { return m_Controls.Count;}
    }

    public GUIControl this[int index]
    {
        get { return (GUIControl)m_Controls[index]; }
    }

    public override void Render()
    {
      if (GUIGraphicsContext.Animations)
      {
        if (m_animator!=null)
        {
          if (m_bStart)
          {
            m_bStart=false;
            StorePosition();
          }

          if (m_animator.IsDone())
          {
            ReStorePosition();
            m_animator=null;
          }
          else
          {
            for (int i=0; i < m_Controls.Count;++i)
            {
              GUIControl cntl=(GUIControl)m_Controls[i];
              cntl.Animate(m_animator);
            }
          }
        }
      }

      for (int i=0; i < m_Controls.Count;++i)
      {
        ((GUIControl)m_Controls[i]).Render();
      }
    }

    public override void FreeResources()
    {
        for (int i=0; i < m_Controls.Count;++i)
        {
          ((GUIControl)m_Controls[i]).FreeResources();
        }
    }

    public override void AllocResources()
    {
      for (int i=0; i < m_Controls.Count;++i)
      {
        ((GUIControl)m_Controls[i]).AllocResources();
      }
    }

    public override void PreAllocResources()
    {
      for (int i=0; i < m_Controls.Count;++i)
      {
        ((GUIControl)m_Controls[i]).PreAllocResources();
      }
    }

    public override GUIControl GetControlById(int ID)
    {
      for (int i=0; i < m_Controls.Count;++i)
      {
        GUIControl cntl = ((GUIControl)m_Controls[i]).GetControlById(ID);
        if (cntl!=null) return cntl;
      }
      return null;
    }

    public override bool NeedRefresh()
    {
      for (int i=0; i < m_Controls.Count;++i)
      {
        if ( ((GUIControl)m_Controls[i]).NeedRefresh() ) return true;
      }
      return false;
    }

    public override bool HitTest(int x, int y, out int controlID, out bool focused)
    {
      controlID=-1;
      focused=false;
      for (int i=0; i < m_Controls.Count;++i)
      {
        if ( ((GUIControl)m_Controls[i]).HitTest(x,y, out controlID,out  focused) ) return true;
      }
      return false;
    }

    public override void OnAction(Action action)
    {
      for (int i=0; i < m_Controls.Count;++i)
      {
        if ( ((GUIControl)m_Controls[i]).Focus ) 
        {
          ((GUIControl)m_Controls[i]).OnAction(action);
        }
      }
    }

    public void Remove(int dwId)
    {
      int index = 0;
      foreach (GUIControl control in m_Controls)
      {
        GUIGroup grp = control as GUIGroup;
        if (grp !=null)
        {
          grp.Remove(dwId);
        }
        else
        {
          if (control.GetID == dwId)
          {
            m_Controls.RemoveAt(index);
            return;
          }
        }
        index++;
      }
    }
    public int GetFocusControlId()
    {
      for (int x = 0; x < m_Controls.Count; ++x)
      {
        GUIGroup grp = m_Controls[x] as GUIGroup;
        if (grp!=null)
        {
          int iFocusedControlId=grp.GetFocusControlId();
          if (iFocusedControlId>=0) return iFocusedControlId;
        }
        else
        {
          if (((GUIControl)m_Controls[x]).Focus) return ((GUIControl)m_Controls[x]).GetID;
        }
      }
      return - 1;
    }

    public override void DoUpdate()
    {
      for (int x = 0; x < m_Controls.Count; ++x)
      {
        ((GUIControl)m_Controls[x]).DoUpdate();
      }
    }

    
    public ArrayList GUIControls
    {
      get 
      {
        return m_Controls;
      }
    }

    public override void StorePosition()
    {
      for (int x = 0; x < m_Controls.Count; ++x)
      {
        ((GUIControl)m_Controls[x]).StorePosition();
      }
      
      base.StorePosition();
    }

    public override void ReStorePosition()
    {
      for (int x = 0; x < m_Controls.Count; ++x)
      {
        ((GUIControl)m_Controls[x]).ReStorePosition();
      }
      
      base.ReStorePosition();
    }

    public override void Animate(Animator animator)
    {
      for (int x = 0; x < m_Controls.Count; ++x)
      {
        ((GUIControl)m_Controls[x]).Animate(animator);
      }
      base.Animate(animator);
    }

	}
}
