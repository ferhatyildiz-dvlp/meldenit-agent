"""Initial schema

Revision ID: 0001
Revises: 
Create Date: 2024-01-01 00:00:00.000000

"""
from alembic import op
import sqlalchemy as sa
from sqlalchemy.dialects import postgresql

# revision identifiers, used by Alembic.
revision = '0001'
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    # Create agents table
    op.create_table('agents',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('agent_guid', sa.String(length=36), nullable=False),
        sa.Column('hostname', sa.String(length=255), nullable=False),
        sa.Column('serial_number', sa.String(length=255), nullable=False),
        sa.Column('domain', sa.String(length=255), nullable=True),
        sa.Column('site_code', sa.String(length=50), nullable=False),
        sa.Column('device_token', sa.String(length=255), nullable=False),
        sa.Column('version', sa.String(length=50), nullable=False),
        sa.Column('status', sa.String(length=50), nullable=True),
        sa.Column('last_heartbeat', sa.DateTime(), nullable=True),
        sa.Column('last_sync', sa.DateTime(), nullable=True),
        sa.Column('created_at', sa.DateTime(), nullable=True),
        sa.Column('updated_at', sa.DateTime(), nullable=True),
        sa.Column('is_online', sa.Boolean(), nullable=True),
        sa.Column('metadata', sa.JSON(), nullable=True),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_agents_id'), 'agents', ['id'], unique=False)
    op.create_index(op.f('ix_agents_agent_guid'), 'agents', ['agent_guid'], unique=True)

    # Create inventories table
    op.create_table('inventories',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('agent_id', sa.Integer(), nullable=False),
        sa.Column('agent_guid', sa.String(length=36), nullable=False),
        sa.Column('sync_type', sa.String(length=20), nullable=False),
        sa.Column('inventory_data', sa.JSON(), nullable=False),
        sa.Column('collected_at', sa.DateTime(), nullable=True),
        sa.Column('synced_at', sa.DateTime(), nullable=True),
        sa.Column('snipeit_updated', sa.Boolean(), nullable=True),
        sa.Column('created_at', sa.DateTime(), nullable=True),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_inventories_id'), 'inventories', ['id'], unique=False)
    op.create_index(op.f('ix_inventories_agent_id'), 'inventories', ['agent_id'], unique=False)
    op.create_index(op.f('ix_inventories_agent_guid'), 'inventories', ['agent_guid'], unique=False)

    # Create heartbeats table
    op.create_table('heartbeats',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('agent_id', sa.Integer(), nullable=False),
        sa.Column('agent_guid', sa.String(length=36), nullable=False),
        sa.Column('status', sa.String(length=50), nullable=False),
        sa.Column('version', sa.String(length=50), nullable=False),
        sa.Column('last_sync', sa.DateTime(), nullable=True),
        sa.Column('received_at', sa.DateTime(), nullable=True),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_heartbeats_id'), 'heartbeats', ['id'], unique=False)
    op.create_index(op.f('ix_heartbeats_agent_id'), 'heartbeats', ['agent_id'], unique=False)
    op.create_index(op.f('ix_heartbeats_agent_guid'), 'heartbeats', ['agent_guid'], unique=False)

    # Create jobs table
    op.create_table('jobs',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('agent_id', sa.Integer(), nullable=False),
        sa.Column('agent_guid', sa.String(length=36), nullable=False),
        sa.Column('job_type', sa.String(length=50), nullable=False),
        sa.Column('status', sa.String(length=50), nullable=True),
        sa.Column('payload', sa.JSON(), nullable=True),
        sa.Column('result', sa.JSON(), nullable=True),
        sa.Column('error_message', sa.Text(), nullable=True),
        sa.Column('created_at', sa.DateTime(), nullable=True),
        sa.Column('started_at', sa.DateTime(), nullable=True),
        sa.Column('completed_at', sa.DateTime(), nullable=True),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_jobs_id'), 'jobs', ['id'], unique=False)
    op.create_index(op.f('ix_jobs_agent_id'), 'jobs', ['agent_id'], unique=False)
    op.create_index(op.f('ix_jobs_agent_guid'), 'jobs', ['agent_guid'], unique=False)

    # Create audit_logs table
    op.create_table('audit_logs',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('agent_id', sa.Integer(), nullable=True),
        sa.Column('agent_guid', sa.String(length=36), nullable=True),
        sa.Column('action', sa.String(length=100), nullable=False),
        sa.Column('resource_type', sa.String(length=50), nullable=False),
        sa.Column('resource_id', sa.String(length=100), nullable=True),
        sa.Column('details', sa.JSON(), nullable=True),
        sa.Column('ip_address', sa.String(length=45), nullable=True),
        sa.Column('user_agent', sa.String(length=500), nullable=True),
        sa.Column('created_at', sa.DateTime(), nullable=True),
        sa.PrimaryKeyConstraint('id')
    )
    op.create_index(op.f('ix_audit_logs_id'), 'audit_logs', ['id'], unique=False)
    op.create_index(op.f('ix_audit_logs_agent_id'), 'audit_logs', ['agent_id'], unique=False)
    op.create_index(op.f('ix_audit_logs_agent_guid'), 'audit_logs', ['agent_guid'], unique=False)


def downgrade() -> None:
    op.drop_index(op.f('ix_audit_logs_agent_guid'), table_name='audit_logs')
    op.drop_index(op.f('ix_audit_logs_agent_id'), table_name='audit_logs')
    op.drop_index(op.f('ix_audit_logs_id'), table_name='audit_logs')
    op.drop_table('audit_logs')
    op.drop_index(op.f('ix_jobs_agent_guid'), table_name='jobs')
    op.drop_index(op.f('ix_jobs_agent_id'), table_name='jobs')
    op.drop_index(op.f('ix_jobs_id'), table_name='jobs')
    op.drop_table('jobs')
    op.drop_index(op.f('ix_heartbeats_agent_guid'), table_name='heartbeats')
    op.drop_index(op.f('ix_heartbeats_agent_id'), table_name='heartbeats')
    op.drop_index(op.f('ix_heartbeats_id'), table_name='heartbeats')
    op.drop_table('heartbeats')
    op.drop_index(op.f('ix_inventories_agent_guid'), table_name='inventories')
    op.drop_index(op.f('ix_inventories_agent_id'), table_name='inventories')
    op.drop_index(op.f('ix_inventories_id'), table_name='inventories')
    op.drop_table('inventories')
    op.drop_index(op.f('ix_agents_agent_guid'), table_name='agents')
    op.drop_index(op.f('ix_agents_id'), table_name='agents')
    op.drop_table('agents')
