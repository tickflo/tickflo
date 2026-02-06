-- migrate:up
-- Standardize index naming convention from mixed styles to consistent idx_* prefix
-- Changes: ix_* -> idx_*, descriptive names -> idx_* pattern

-- Rename ix_* indexes to idx_*
ALTER INDEX IF EXISTS public.ix_contact_locations_workspace_contact 
    RENAME TO idx_contact_locations_workspace_contact;

-- Rename descriptive index names to follow idx_* pattern
ALTER INDEX IF EXISTS public.teams_workspace_id_name_idx 
    RENAME TO idx_teams_workspace_name;

ALTER INDEX IF EXISTS public.report_runs_workspace_report_idx 
    RENAME TO idx_report_runs_workspace_report;

-- migrate:down
-- Revert to original index names for rollback

ALTER INDEX IF EXISTS public.idx_contact_locations_workspace_contact 
    RENAME TO ix_contact_locations_workspace_contact;

ALTER INDEX IF EXISTS public.idx_teams_workspace_name 
    RENAME TO teams_workspace_id_name_idx;

ALTER INDEX IF EXISTS public.idx_report_runs_workspace_report 
    RENAME TO report_runs_workspace_report_idx;
