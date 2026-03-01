// Integration event message contracts are defined in the Application layer so they
// can be referenced by both Application handlers (publishers) and Infrastructure
// consumers without circular project references.
//
// See: AppLogica.Desk.Application.Common.IntegrationEvents
//
// This file re-exports them into the Infrastructure.EventBus namespace for
// backward-compatibility and convenience within Infrastructure code.

global using AppLogica.Desk.Application.Common.IntegrationEvents;
