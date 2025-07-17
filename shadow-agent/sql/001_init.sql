create table public.scope (
    id serial primary key,
    name text not null unique,
    messenger_channel_id text not null,
    notify_reason integer not null,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now()
);

CREATE FUNCTION public.create_scope(p_name text, p_messenger_channel_id text, p_notify_reason integer) 
RETURNS TABLE (
    id integer,
    name text,
    messenger_channel_id text,
    notify_reason integer,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
AS $$
BEGIN
    RETURN QUERY 
    INSERT INTO public.scope as s (name, messenger_channel_id, notify_reason) 
    VALUES (p_name, p_messenger_channel_id, p_notify_reason) 
    RETURNING s.id, s.name, s.messenger_channel_id, s.notify_reason, s.created_at, s.updated_at;
END;
$$ LANGUAGE plpgsql;


CREATE FUNCTION public.update_scope(p_name text, p_messenger_channel_id text, p_notify_reason integer) 
RETURNS TABLE (
    id integer,
    name text,
    messenger_channel_id text,
    notify_reason integer,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
AS $$
BEGIN
    RETURN QUERY 
    UPDATE public.scope as s 
    SET messenger_channel_id = p_messenger_channel_id, notify_reason = p_notify_reason, updated_at = now()
    WHERE s.name = p_name 
    RETURNING s.id, s.name, s.messenger_channel_id, s.notify_reason, s.created_at, s.updated_at;
END;
$$ LANGUAGE plpgsql;

CREATE FUNCTION public.get_scope(p_name text) 
RETURNS TABLE (
    id integer,
    name text,
    messenger_channel_id text,
    notify_reason integer,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
AS $$
BEGIN
    RETURN QUERY 
    SELECT * FROM public.scope as s 
    WHERE s.name = p_name;
END;
$$ LANGUAGE plpgsql;