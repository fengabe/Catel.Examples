﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EmployeesViewModel.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Examples.WPF.Prism.Modules.Employees.ViewModels
{
    using Collections;
    using Data;
    using IoC;
    using MVVM;
    using MVVM.Services;
    using Messaging;
    using Models;
    using Services;
    using ViewModelBase = Prism.ViewModels.ViewModelBase;

    /// <summary>
    /// Employees view model.
    /// </summary>
    public class EmployeesViewModel : ViewModelBase
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EmployeesViewModel"/> class.
        /// </summary>
        public EmployeesViewModel()
        {
            AddEmployee = new Command(OnAddEmployeeExecute);
            EditEmployee = new Command(OnEditEmployeeExecute, OnEditEmployeeCanExecute);
            DeleteEmployee = new Command(OnDeleteEmployeeExecute, OnDeleteEmployeeCanExecute);

            Employees = new FastObservableCollection<IEmployee>();
            if (!ObjectHelper.IsNull(SelectedDepartment))
            {
                Employees.AddRange(EmployeeRepository.GetAllEmployees(SelectedDepartment.Name));
            }

            if (Employees.Count > 0)
            {
                SelectedEmployee = Employees[0];
            }

            Mediator.Register<string>(this, OnSelectedDepartmentUpdated, "UpdateEmployees");
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the title of the view model.
        /// </summary>
        /// <value>The title.</value>
        public override string Title
        {
            get { return "Employees"; }
        }

        private static IEmployeeRepository EmployeeRepository
        {
            get { return IoC.ServiceLocator.Default.ResolveType<IEmployeeRepository>(); }
        }

        /// <summary>
        /// Gets the selected department.
        /// </summary>
        private IDepartment SelectedDepartment
        {
            get { return IoC.ServiceLocator.Default.ResolveType<IDepartmentTracker>().CurrentDepartment; }
        }

        /// <summary>
        /// Gets the mediator.
        /// </summary>
        private static IMessageMediator Mediator
        {
            get { return IoC.ServiceLocator.Default.ResolveType<IMessageMediator>(); }
        }

        /// <summary>
        /// Gets or sets the selected employee.
        /// </summary>
        public IEmployee SelectedEmployee
        {
            get { return GetValue<IEmployee>(SelectedEmployeeProperty); }
            set { SetValue(SelectedEmployeeProperty, value); }
        }

        /// <summary>
        /// Register the SelectedEmployee property so it is known in the class.
        /// </summary>
        public static readonly PropertyData SelectedEmployeeProperty = RegisterProperty("SelectedEmployee", typeof (IEmployee));

        /// <summary>
        /// Gets or sets the employees.
        /// </summary>
        public FastObservableCollection<IEmployee> Employees
        {
            get { return GetValue<FastObservableCollection<IEmployee>>(EmployeesProperty); }
            private set { SetValue(EmployeesProperty, value); }
        }

        /// <summary>
        /// Register the Employees property so it is known in the class.
        /// </summary>
        public static readonly PropertyData EmployeesProperty = RegisterProperty("Employees", typeof (FastObservableCollection<IEmployee>));
        #endregion

        #region Commands
        /// <summary>
        /// Gets the AddEmployee command.
        /// </summary>
        public Command AddEmployee { get; private set; }

        /// <summary>
        /// Method to invoke when the AddEmployee command is executed.
        /// </summary>
        private void OnAddEmployeeExecute()
        {
            var employee = new Employee() {Department = SelectedDepartment};
            var viewModel = new EmployeeViewModel(employee);

            var uiVisualizerService = GetService<IUIVisualizerService>();
            if (!(uiVisualizerService.ShowDialog(viewModel) ?? false))
            {
                return;
            }

            var employeeRepository = GetService<IEmployeeRepository>();
            employeeRepository.AddEmployee(employee);

            if (employee.Department == SelectedDepartment)
            {
                Employees.Add(employee);
            }

            MessageMediator.SendMessage(employee.Department, "UpdateSelectedDepartmentFromEM");
            Mediator.SendMessage(string.Format("Employee {0} {1} is added in department {2}", employee.FirstName, employee.LastName, employee.Department.Name), "UpdateNotification");
        }

        /// <summary>
        /// Gets the EditEmployee command.
        /// </summary>
        public Command EditEmployee { get; private set; }

        /// <summary>
        /// Method to check whether the EditEmployee command can be executed.
        /// </summary>
        /// <returns><c>true</c> if the command can be executed; otherwise <c>false</c></returns>
        private bool OnEditEmployeeCanExecute()
        {
            return !ObjectHelper.IsNull(SelectedEmployee);
        }

        /// <summary>
        /// Method to invoke when the EditEmployee command is executed.
        /// </summary>
        private void OnEditEmployeeExecute()
        {
            var viewModel = new EmployeeViewModel(SelectedEmployee);
            var uiVisualizerService = GetService<IUIVisualizerService>();
            uiVisualizerService.ShowDialog(viewModel);
            if (SelectedDepartment != null)
            {
                OnSelectedDepartmentUpdated(SelectedDepartment.Name);
            }
        }

        /// <summary>
        /// Gets the DeleteEmployee command.
        /// </summary>
        public Command DeleteEmployee { get; private set; }

        /// <summary>
        /// Method to check whether the DeleteEmployee command can be executed.
        /// </summary>
        /// <returns><c>true</c> if the command can be executed; otherwise <c>false</c></returns>
        private bool OnDeleteEmployeeCanExecute()
        {
            return !ObjectHelper.IsNull(SelectedEmployee);
        }

        /// <summary>
        /// Method to invoke when the DeleteEmployee command is executed.
        /// </summary>
        private void OnDeleteEmployeeExecute()
        {
            if (ObjectHelper.IsNull(SelectedEmployee))
                return;

            var employee = SelectedEmployee;

            var messageService = GetService<IMessageService>();
            if (
                messageService.Show(
                    string.Format("Are you sure to delete {0} {1}", employee.FirstName, employee.LastName),
                    "Are you sure?", MessageButton.YesNo) != MessageResult.Yes)
            {
                return;
            }
            Employees.Remove(employee);
            EmployeeRepository.DeleteEmployee(employee);
            SelectedEmployee = null;

            Mediator.SendMessage(string.Format("Employee {0} {1} is deleted", employee.FirstName, employee.LastName), "UpdateNotification");
        }
        #endregion

        #region Methods
        /// <summary>
        /// Called when the selected department is updated.
        /// </summary>
        /// <param name="data">The data.</param>
        private void OnSelectedDepartmentUpdated(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            Employees.ReplaceRange(EmployeeRepository.GetAllEmployees(data));
            SelectedEmployee = (Employees.Count > 0) ? Employees[0] : null;

            Mediator.SendMessage(string.Format("Department \"{0}\" is loaded with {1} rows", data, Employees.Count), "UpdateNotification");
        }
        #endregion
    }
}