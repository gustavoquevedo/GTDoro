﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using GTDoro.Core.Models;
using System.Drawing;

namespace GTDoro.Core.Models
{
    public class Action : PomodoroContainer
    {
        public Action()
        {
            Pomodoros = new HashSet<Pomodoro>();
        }

        public int ID { get; set; }

        [Required(ErrorMessage="Name is required")]
        [StringLength(Settings.PTA_NAME_MAX_LENGTH, ErrorMessage = Settings.VAL_ERR_NAME_MAX_MSG)]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        [StringLength(Settings.PTA_DESCRIPTION_MAX_LENGTH, ErrorMessage = Settings.VAL_ERR_DESC_MAX_MSG)]
        public string Description { get; set; }

        [Display(Name = "Creation Date")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public override DateTime? CreationDate { get; set; }

        /// <summary>
        /// Date when the action was completed
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [UIHint("Date")]
        [DataType(DataType.DateTime)]
        [Display(Name = "End Date")]
        public override DateTime? EndDate { get; set; }

        public int? Estimate { get; set; }

        /// <summary>
        /// Date when the action is supposed to be completed
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [UIHint("Date")]
        [DataType(DataType.DateTime)]
        public DateTime? Deadline { get; set; }

        public override Status Status { get; set; }

        [Display(Name = "Recurrent")]
        public bool IsPersistent { get; set; }
        
        public LevelExtended Priority { get; set; }

        [Required] 
        public int TaskID { get; set; }

        public virtual Task Task { get; set; }

        public void SetName(string Name) 
        {
            Name = Name ?? string.Empty;
            this.Name = Name.Substring(0, Math.Min(Settings.PTA_NAME_MAX_LENGTH, Name.Length)); 
        }

        [Display(Name = "Work units")]
        public virtual ICollection<Pomodoro> Pomodoros { get; set; }
        
        public override ICollection<Pomodoro> GetPomodoros()
        {
            //just in case Pomodoros is null when creating
            return Pomodoros ?? new Pomodoro[0];
        }

        [Display(Name = "Is Expired")]
        public bool IsExpired
        {
            get
            {
                return Deadline.HasValue && (Deadline.Value.Date < DateTime.Today.Date);
            }
        }

        [Display(Name = "Ext. Status")]
        public ActionExtendedStatus ExtendedStatus
        {
            get
            {
                if (InheritedStatus == false)
                {
                    switch (Status)
                    {
                        case Status.Cancelled:
                            return ActionExtendedStatus.Cancelled;
                        case Status.Completed:
                            return ActionExtendedStatus.Completed;
                        case Status.Active:
                            if (CompletedPomodorosCount == 0)
                            {
                                return ActionExtendedStatus.Created;
                            }
                            else if (Estimate < CompletedPomodorosCount)
                            {
                                return ActionExtendedStatus.Exceeded;
                            }
                            break;
                    }
                }
                else
                {
                    switch (this.Task.ExtendedStatus)
                    {
                        case TaskExtendedStatus.Cancelled:
                        case TaskExtendedStatus.CancelledInherited:
                            return ActionExtendedStatus.CancelledInherited;
                        case TaskExtendedStatus.Completed:
                        case TaskExtendedStatus.CompletedInherited:
                            return ActionExtendedStatus.CompletedInherited;
                        case TaskExtendedStatus.OnHold:
                        case TaskExtendedStatus.OnHoldInherited:
                            return ActionExtendedStatus.OnHoldInherited;
                    }
                }
                return ActionExtendedStatus.InProgress;
            }
        }

        public DateTime? DeadlineOrEndDate
        {
            get
            {
                if (Status == Status.Completed && EndDate.HasValue)
                {
                    return EndDate;
                }
                if (Status == Status.Active && Deadline.HasValue)
                {
                    return Deadline;
                }
                return null;
            }
        }
        
        public string GetDeadlineOrEndDateIconHtmlTag(bool shortVersion = false)
        {
            if(Status == Status.Completed && EndDate.HasValue)
            {
                return "<i class=\"fa fa-fw " + Settings.ICON_COMPLETED + "\" title=\"Completed: "
                    + EndDate.Value.Date.ToShortDateString() + "\"></i>";
            }
            if (Status == Status.Active && Deadline.HasValue)
            {
                return "<i class=\"fa fa-fw " + Settings.ICON_DEADLINE + "\" title=\" Deadline: "
                    + Deadline.Value.Date.ToShortDateString() + "\"></i>";
            }
            return string.Empty;
        }

        public bool IsSelectedAction { get { return Owner != null && ID == Owner.ActionID; } }

        public override decimal? Effort
        {
            get
            {
                if (Estimate.HasValue && Estimate > 0)
                {
                    return 100M * ((decimal)CompletedPomodorosCount / (decimal)Estimate.Value);
                }
                return null;
            }
        }
        
        public override bool IsSelectable
        {
            get
            {
                return IsActive && 
                    Task != null && Task.IsActive && 
                    Task.Project != null && Task.Project.IsActive;
            }
        }

        public override string PathItemName
        {
            get
            {
                return ItemName + " / " + Parent.PathItemName;
            }
        }

        public override string ItemName
        {
            get
            {
                return Name;
            }
        }

        public override PomodoroContainerType Type
        {
            get
            {
                return PomodoroContainerType.Action;
            }
        }

        public override int Ident
        {
            get { return ID; }
        }

        public override String CssClass
        {
            get { return "gt-action" + (IsActive ? " gt-active" : string.Empty); }
        }

        public override int EstimateWork
        {
            get 
            { 
                if (IsPersistent == false &&
                    Status != Status.Cancelled && 
                    Estimate.HasValue)
                {
                    return Estimate.Value;
                }
                return 0; 
            }
        }

        public override int CompletedPomodorosIfEstimate
        {
            get { return Estimate.HasValue ? CompletedPomodorosCount : 0; }
        }

        public override bool InheritedStatus
        {
            get
            {
                return  Status == Status.Active && 
                        Task != null && (Task.InheritedStatus || Task.IsActive == false);
            }
        }

        public override bool ContainsSelectedAction
        {
            get 
            {
                if (Owner != null)
                {
                    return Owner.ActionID == ID; 
                }
                return false;
            }
        }

        public override Identity.ApplicationUser Owner
        {
            get 
            {
                if (Task != null && Task.Project != null)
                {
                    return Task.Project.User;
                }
                return null;
            }
        }

        public override LoggableItemContainer Parent { get { return Task; } }

        public override PomodoroContainer NextSibling
        {
            get 
            {           
                Action sibling = Task.Actions.OrderBy(a => a.ID).SkipWhile(a => a.ID != ID).Skip(1).FirstOrDefault();
                if (sibling == null)
                {
                    //end of list, get first
                    sibling = Task.Actions.OrderBy(p => p.ID).FirstOrDefault();
                    //itself, not a sibling
                    if (sibling.ID == ID)
                    {
                        return null;
                    }
                }
                return sibling;
            }
        }

        public static bool IsFinishedStatus(Status status)
        {
            return status == Status.Completed || status == Status.Cancelled;
        }

        public override bool IsFinished
        {
            get { return IsFinishedStatus(Status); }
        }

        public override Color TypeColor
        {
            get { return Color.DarkGreen; }
        }
    }
}