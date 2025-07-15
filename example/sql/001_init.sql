create table public.entity (
    id serial primary key,
    name text not null,
    created_at timestamp with time zone not null default now()
);

CREATE FUNCTION public.create_entity(p_name text) 
RETURNS TABLE (
    id integer,
    name text,
    created_at timestamp with time zone
)
AS $$
BEGIN
    RETURN QUERY 
    INSERT INTO public.entity as e (name) 
    VALUES (p_name) 
    RETURNING e.id, e.name, e.created_at;
END;
$$ LANGUAGE plpgsql;